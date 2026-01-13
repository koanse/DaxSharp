using System.Data;
using Npgsql;
using DaxSharp.Helpers;
using DaxSharp.Models;
using DaxSharp.Cache;
using DaxSharp.Comparison;
using DaxSharp.OpenAI;
using DaxSharp.Postgres;

namespace DaxSharp;

public static class DaxSharpPbiExportToPostgres
{
    /// <summary>
    /// Exports all tables from PowerBI to PostgreSQL.
    /// Creates tables in PostgreSQL based on PBI table structure and exports data.
    /// </summary>
    /// <param name="pbiConnectionString">AdomdConnection connection string for PowerBI (e.g., "Data Source=localhost:55023;")</param>
    /// <param name="postgresConnectionString">PostgreSQL connection string (e.g., "Host=localhost;Database=mydb;Username=user;Password=pass")</param>
    /// <param name="schemaName">PostgreSQL schema name (default: "public")</param>
    /// <returns>Dictionary with table names as keys and number of exported rows as values</returns>
    public static Dictionary<string, int> ExportAllTables(
        string pbiConnectionString,
        string postgresConnectionString,
        string schemaName = "")
    {
        var config = DaxSharpConfig.Instance;
        if (string.IsNullOrEmpty(schemaName))
        {
            schemaName = config.PostgreSql.DefaultSchema;
        }
        
        var results = new Dictionary<string, int>();
        var tables = DaxSharpPbiExportExtensions.GetTables(pbiConnectionString);

        using var postgresConnection = new NpgsqlConnection(postgresConnectionString);
        postgresConnection.Open();

        // Ensure schema exists
        PostgresSchemaManager.EnsureSchema(postgresConnection, schemaName);

        foreach (var tableName in tables.Where(tableName => !tableName.Equals(config.PowerBi.MeasuresTableName, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var rowCount = ExportTable(
                    pbiConnectionString,
                    postgresConnectionString,
                    tableName,
                    schemaName);
                results[tableName] = rowCount;
            }
            catch (Exception ex)
            {
                // Log error but continue with other tables
                Console.WriteLine($"Error exporting table {tableName}: {ex.Message}");
                results[tableName] = -1; // -1 indicates error
            }
        }

        return results;
    }

    /// <summary>
    /// Exports a single table from PowerBI to PostgreSQL.
    /// </summary>
    /// <param name="pbiConnectionString">AdomdConnection connection string for PowerBI</param>
    /// <param name="postgresConnectionString">PostgreSQL connection string</param>
    /// <param name="tableName">Name of the table to export</param>
    /// <param name="schemaName">PostgreSQL schema name (default: "public")</param>
    /// <returns>Number of exported rows</returns>
    private static int ExportTable(
        string pbiConnectionString,
        string postgresConnectionString,
        string tableName,
        string schemaName = "")
    {
        var config = DaxSharpConfig.Instance;
        if (string.IsNullOrEmpty(schemaName))
        {
            schemaName = config.PostgreSql.DefaultSchema;
        }
        
        // Get table structure from PBI
        var tableStructure = GetTableStructure(pbiConnectionString, tableName);
        
        if (tableStructure.Columns.Count == 0)
        {
            return 0;
        }

        // Create table in PostgreSQL
        PostgresSchemaManager.CreatePostgresTable(postgresConnectionString, schemaName, tableName, tableStructure);

        // Export data
        return PostgresDataExporter.ExportTableData(pbiConnectionString, postgresConnectionString, schemaName, tableName, tableStructure);
    }

    /// <summary>
    /// Gets the structure of a table from PowerBI.
    /// </summary>
    private static DataTable GetTableStructure(string pbiConnectionString, string tableName)
    {
        // Create DataTable manually from schema metadata to avoid constraint issues
        var dataTable = new DataTable();
        
        // Execute a query to get table structure from metadata
        // Escape single quotes in table name for DAX (double them)
        var escapedTableName = DaxIdentifierHelper.EscapeDaxIdentifier(tableName);
        var config = DaxSharpConfig.Instance;
        var daxQuery = string.Format(config.PowerBi.DaxQueryTemplates.GetTableStructure, escapedTableName);
        
        using var connection = new Microsoft.AnalysisServices.AdomdClient.AdomdConnection(pbiConnectionString);
        connection.Open();

        using var command = new Microsoft.AnalysisServices.AdomdClient.AdomdCommand(daxQuery, connection);
        command.CommandTimeout = config.PowerBi.CommandTimeout;

        using var reader = command.ExecuteReader();
        
        // Get schema information without loading data
        var schemaTable = reader.GetSchemaTable();
        
        if (schemaTable != null)
        {
            // Create columns from schema metadata
            foreach (DataRow schemaRow in schemaTable.Rows)
            {
                var columnName = schemaRow["ColumnName"]?.ToString() ?? string.Empty;
                // Clean column name: remove table prefix and square brackets
                columnName = DaxIdentifierHelper.CleanColumnName(columnName);
                var dataType = (Type)(schemaRow["DataType"] ?? typeof(string));
                
                var column = new DataColumn(columnName, dataType)
                {
                    AllowDBNull = true // Always allow null to avoid constraint issues
                };
                
                dataTable.Columns.Add(column);
            }
        }
        else
        {
            // Fallback: if schema table is not available, read one row to get structure
            // but don't load it into DataTable to avoid constraints
            if (!reader.Read())
            {
                return dataTable;
            }

            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                // Clean column name: remove table prefix and square brackets
                columnName = DaxIdentifierHelper.CleanColumnName(columnName);
                var dataType = reader.GetFieldType(i) ?? typeof(string);
                    
                var column = new DataColumn(columnName, dataType)
                {
                    AllowDBNull = true
                };
                    
                dataTable.Columns.Add(column);
            }
        }
        
        return dataTable;
    }

    /// <summary>
    /// Converts DAX query to PostgreSQL SQL using OpenAI API, executes both queries,
    /// compares results, and iteratively fixes SQL if results don't match.
    /// Uses cache to avoid redundant API calls.
    /// </summary>
    /// <param name="daxQuery">DAX query to convert (e.g., "EVALUATE SUMMARIZECOLUMNS(...)")</param>
    /// <param name="pbiConnectionString">AdomdConnection connection string for PowerBI</param>
    /// <param name="postgresConnectionString">PostgreSQL connection string</param>
    /// <param name="schemaName">PostgreSQL schema name (default: "public")</param>
    /// <param name="apiKey">OpenAI API key</param>
    /// <param name="model">OpenAI model to use (default: "gpt-4")</param>
    /// <param name="maxIterations">Maximum number of iterations to fix SQL (default: 3)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Conversion result with SQL query and comparison results</returns>
    public static async Task<DaxToSqlConversionResult> ConvertDaxToSqlWithValidation(
        string daxQuery,
        string pbiConnectionString,
        string postgresConnectionString,
        string schemaName = "",
        string apiKey = "",
        string model = "",
        int maxIterations = 0,
        CancellationToken cancellationToken = default)
    {
        var config = DaxSharpConfig.Instance;
        
        // Use defaults from config if not provided
        if (string.IsNullOrEmpty(schemaName))
        {
            schemaName = config.PostgreSql.DefaultSchema;
        }
        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = config.OpenAi.DefaultApiKey;
        }
        if (string.IsNullOrEmpty(model))
        {
            model = config.OpenAi.DefaultModel;
        }
        if (maxIterations == 0)
        {
            maxIterations = config.OpenAi.DefaultMaxIterations;
        }
        
        // Create cache request object
        var cacheRequest = new CacheRequest
        {
            DaxQuery = daxQuery,
            PbiConnectionString = pbiConnectionString,
            PostgresConnectionString = postgresConnectionString,
            SchemaName = schemaName,
            ApiKey = apiKey,
            Model = model,
            MaxIterations = maxIterations
        };

        // Try to load from cache
        var cachedResult = DaxToSqlCache.LoadFromCache(cacheRequest);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var result = new DaxToSqlConversionResult();
        
        try
        {
            // Step 0: Get database schema (tables, columns, relationships)
            var databaseSchema = DaxSharpPbiExportExtensions.GetDatabaseSchema(pbiConnectionString);
            var schemaDescription = SchemaFormatter.FormatSchemaForPrompt(databaseSchema, schemaName);
            
            // Step 1: Execute DAX query in PowerBI and get results
            var powerBiResults = DaxSharpPbiExportExtensions.ExecuteDaxQuery(pbiConnectionString, daxQuery);
            result.PowerBiResults = powerBiResults;
            
            if (powerBiResults.Count == 0)
            {
                result.ErrorMessage = config.ErrorMessages.DaxQueryNoResults;
                return result;
            }

            // Step 2: Generate SQL from DAX using OpenAI
            var sqlQuery = string.Empty;
            var iteration = 0;
            var previousError = string.Empty;
            
            while (iteration < maxIterations)
            {
                iteration++;
                result.Iterations = iteration;
                
                try
                {
                    // Build prompt for OpenAI
                    var openAiConfig = config.OpenAi;
                    string prompt;
                    
                    if (iteration == 1)
                    {
                        prompt = string.Format(openAiConfig.Prompts.InitialConversion, schemaDescription, daxQuery, schemaName);
                    }
                    else
                    {
                        var guidance = string.Empty;
                        
                        // Add specific guidance based on error type
                        if (previousError.Contains("duplicate grouping key", StringComparison.OrdinalIgnoreCase) ||
                            previousError.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase))
                        {
                            guidance = openAiConfig.Prompts.GroupByGuidance;
                        }
                        else if (previousError.Contains("too many rows", StringComparison.OrdinalIgnoreCase))
                        {
                            guidance = openAiConfig.Prompts.TooManyRowsGuidance;
                        }
                        
                        prompt = string.Format(openAiConfig.Prompts.FixQuery, schemaDescription, daxQuery, sqlQuery, previousError, schemaName, guidance);
                    }
                    
                    // Check if API key is provided
                    if (string.IsNullOrEmpty(apiKey))
                    {
                        result.ErrorMessage = "OpenAI API key is required. Please provide apiKey parameter or set it in appsettings.json (OpenAI:DefaultApiKey).";
                        return result;
                    }
                    
                    // Call OpenAI API
                    sqlQuery = await OpenAiService.GenerateSqlFromPrompt(prompt, apiKey, model, cancellationToken);
                    
                    if (string.IsNullOrEmpty(sqlQuery))
                    {
                        result.ErrorMessage = config.ErrorMessages.FailedToGenerateSql;
                        return result;
                    }
                    
                    // Step 3: Execute SQL in PostgreSQL
                    var postgresResults = PostgresQueryExecutor.ExecutePostgresQuery(postgresConnectionString, sqlQuery);
                    result.PostgresResults = postgresResults;
                    
                    // Step 4: Compare results
                    var comparison = ResultComparer.CompareResults(powerBiResults, postgresResults);
                    
                    if (comparison.Match)
                    {
                        result.SqlQuery = sqlQuery;
                        result.ResultsMatch = true;
                        if (string.IsNullOrEmpty(result.ErrorMessage))
                        {
                            DaxToSqlCache.SaveToCache(cacheRequest, result);
                        }
                        return result;
                    }
                    else
                    {
                        // Prepare error message for next iteration
                        previousError = $"Results don't match. {comparison.DifferenceDescription}";
                    }
                }
                catch (Exception ex)
                {
                    previousError = SqlErrorFormatter.FormatSqlError(ex, sqlQuery);
                    if (iteration < maxIterations)
                    {
                        continue;
                    }

                    result.ErrorMessage = previousError;
                    result.SqlQuery = sqlQuery;
                    return result;
                }
            }
            
            // If we get here, we've exhausted iterations
            result.SqlQuery = sqlQuery;
            result.ResultsMatch = false;
            result.ErrorMessage = previousError;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Error during conversion: {ex.Message}";
        }
        
        // Save to cache only if results match successfully
        // Don't cache failed conversions to allow retry with different approaches
        if (result.ResultsMatch && string.IsNullOrEmpty(result.ErrorMessage))
        {
            DaxToSqlCache.SaveToCache(cacheRequest, result);
        }
        
        return result;
    }
}
