using System.Data;
using System.Text;
using Microsoft.AnalysisServices.AdomdClient;
using DaxSharp.Helpers;
using DaxSharp.Models;

namespace DaxSharp;

public static class DaxSharpPbiExportExtensions
{
    /// <summary>
    /// Executes a DAX query against PowerBI/Analysis Services and returns the results as a list of dictionaries.
    /// </summary>
    /// <param name="connectionString">AdomdConnection connection string (e.g., "Data Source=localhost:55023;")</param>
    /// <param name="daxQuery">DAX query to execute (e.g., "EVALUATE 'TableName'")</param>
    /// <returns>List of dictionaries where each dictionary represents a row with column names as keys</returns>
    public static List<Dictionary<string, object?>> ExecuteDaxQuery(string connectionString, string daxQuery)
    {
        var results = new List<Dictionary<string, object?>>();

        using var connection = new AdomdConnection(connectionString);
        connection.Open();

        using var command = new AdomdCommand(daxQuery, connection);
        command.CommandTimeout = DaxSharpConfig.Instance.PowerBi.CommandTimeout; // No timeout

        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                // Clean column name: remove table prefix and square brackets
                var cleanedColumnName = DaxIdentifierHelper.CleanColumnName(columnName);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                // Store only with cleaned name to avoid duplicates
                // If cleaned name conflicts with existing key, use original name as fallback
                var finalKey = cleanedColumnName;
                if (row.ContainsKey(finalKey))
                {
                    // Conflict detected - use original name if different, otherwise append index
                    finalKey = cleanedColumnName != columnName ? columnName : $"{cleanedColumnName}_{i}";
                }
                // Only add if key doesn't exist (to prevent duplicates)
                row.TryAdd(finalKey, value);
            }
            results.Add(row);
        }

        return results;
    }

    /// <summary>
    /// Executes a DAX query and returns results as an async enumerable for streaming large datasets.
    /// Note: AdomdConnection doesn't support async operations, so this method uses synchronous operations wrapped in Task.
    /// </summary>
    /// <param name="connectionString">AdomdConnection connection string</param>
    /// <param name="daxQuery">DAX query to execute</param>
    /// <returns>Async enumerable of dictionaries representing rows</returns>
    public static async IAsyncEnumerable<Dictionary<string, object?>> ExecuteDaxQueryAsync(
        string connectionString, 
        string daxQuery)
    {
        using var connection = new AdomdConnection(connectionString);
        connection.Open();

        using var command = new AdomdCommand(daxQuery, connection);
        command.CommandTimeout = DaxSharpConfig.Instance.PowerBi.CommandTimeout;

        using var reader = command.ExecuteReader();
        
        // Use Task.Run to make it async-friendly, but note that the underlying operations are synchronous
        await Task.Yield(); // Yield to allow async enumeration
        
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                // Clean column name: remove table prefix and square brackets
                var cleanedColumnName = DaxIdentifierHelper.CleanColumnName(columnName);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                // Store with cleaned name, but also keep original if different
                row[cleanedColumnName] = value;
                if (cleanedColumnName != columnName)
                {
                    row[columnName] = value; // Keep original for backward compatibility
                }
            }
            yield return row;
        }
    }

    /// <summary>
    /// Executes a DAX query and maps results to strongly-typed objects.
    /// </summary>
    /// <typeparam name="T">Type to map rows to</typeparam>
    /// <param name="connectionString">AdomdConnection connection string</param>
    /// <param name="daxQuery">DAX query to execute</param>
    /// <param name="mapper">Function to map dictionary to type T</param>
    /// <returns>List of mapped objects</returns>
    public static List<T> ExecuteDaxQuery<T>(
        string connectionString, 
        string daxQuery, 
        Func<Dictionary<string, object?>, T> mapper)
    {
        var results = ExecuteDaxQuery(connectionString, daxQuery);
        return results.Select(mapper).ToList();
    }

    /// <summary>
    /// Executes a DAX query and returns results as a DataTable.
    /// </summary>
    /// <param name="connectionString">AdomdConnection connection string</param>
    /// <param name="daxQuery">DAX query to execute</param>
    /// <returns>DataTable with query results</returns>
    public static DataTable ExecuteDaxQueryAsDataTable(string connectionString, string daxQuery)
    {
        var dataTable = new DataTable();
        
        // Disable constraints to avoid issues when loading data
        dataTable.BeginLoadData();

        using var connection = new AdomdConnection(connectionString);
        connection.Open();

        using var command = new AdomdCommand(daxQuery, connection);
        command.CommandTimeout = DaxSharpConfig.Instance.PowerBi.CommandTimeout;

        using var reader = command.ExecuteReader();
        dataTable.Load(reader);
        
        // Clear any constraints that were automatically created during load
        dataTable.Constraints.Clear();
        
        // Set all columns to allow null to avoid constraint violations
        foreach (DataColumn column in dataTable.Columns)
        {
            column.AllowDBNull = true;
        }
        
        // Re-enable load data mode
        dataTable.EndLoadData();

        return dataTable;
    }

    /// <summary>
    /// Tests the connection to PowerBI/Analysis Services.
    /// </summary>
    /// <param name="connectionString">AdomdConnection connection string</param>
    /// <returns>True if connection is successful, false otherwise</returns>
    public static bool TestConnection(string connectionString)
    {
        try
        {
            using var connection = new AdomdConnection(connectionString);
            connection.Open();
            return connection.State == ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the list of tables/cubes available in the PowerBI/Analysis Services database.
    /// </summary>
    /// <param name="connectionString">AdomdConnection connection string</param>
    /// <returns>List of table/cube names</returns>
    public static List<string> GetTables(string connectionString)
    {
        var tables = new List<string>();

        using var connection = new AdomdConnection(connectionString);
        connection.Open();

        // Primary method: Use AdomdConnection.Cubes collection
        // In PowerBI tabular models, tables are typically accessible as dimensions or via the main cube
        try
        {
            // Get the main cube (usually "Model" in PowerBI)
            var config = DaxSharpConfig.Instance.PowerBi;
            CubeDef? mainCube = null;
            foreach (var cube in connection.Cubes)
            {
                if (cube.Name.StartsWith(config.SystemObjectPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                mainCube = cube;
                break;
            }

            if (mainCube != null)
            {
                // In tabular models, tables are represented as Dimensions
                foreach (var dimension in mainCube.Dimensions)
                {
                    if (dimension.Name != null &&
                        !dimension.Name.StartsWith(config.SystemObjectPrefix, StringComparison.OrdinalIgnoreCase) &&
                        !dimension.Name.Equals(config.ModelTableName, StringComparison.OrdinalIgnoreCase) &&
                        !dimension.Name.Equals(config.MeasuresTableName, StringComparison.OrdinalIgnoreCase) &&
                        !tables.Contains(dimension.Name))
                    {
                        tables.Add(dimension.Name);
                    }
                }
            }

            // If no tables found via Dimensions, try using cube names directly (excluding Model and Measures)
            if (tables.Count == 0)
            {
                foreach (CubeDef cube in connection.Cubes)
                {
                    if (cube.Name != null && 
                        !cube.Name.StartsWith(config.SystemObjectPrefix, StringComparison.OrdinalIgnoreCase) &&
                        !cube.Name.Equals(config.ModelTableName, StringComparison.OrdinalIgnoreCase) &&
                        !cube.Name.Equals(config.MeasuresTableName, StringComparison.OrdinalIgnoreCase) &&
                        !tables.Contains(cube.Name))
                    {
                        tables.Add(cube.Name);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing Cubes/Dimensions: {ex.Message}");
        }

        return tables;
    }

    /// <summary>
    /// Gets the columns/measures for a specific table.
    /// </summary>
    /// <param name="connectionString">AdomdConnection connection string</param>
    /// <param name="tableName">Name of the table</param>
    /// <returns>List of column/measure names</returns>
    public static List<string> GetColumns(string connectionString, string tableName)
    {
        var columns = new List<string>();

        using var connection = new AdomdConnection(connectionString);
        connection.Open();

        // Try to get a sample row to determine columns
        try
        {
            var escapedTableName = DaxIdentifierHelper.EscapeDaxIdentifier(tableName);
            var config = DaxSharpConfig.Instance.PowerBi;
            var daxQuery = string.Format(config.DaxQueryTemplates.GetTableStructure, escapedTableName);
            using var command = new AdomdCommand(daxQuery, connection);
            using var reader = command.ExecuteReader();
            
            if (reader.Read())
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    if (!string.IsNullOrEmpty(columnName))
                    {
                        columns.Add(columnName);
                    }
                }
            }
        }
        catch
        {
            // If query fails, return empty list
        }

        return columns;
    }

    /// <summary>
    /// Executes a DAX query and returns results as an enumerable for lazy evaluation.
    /// This method is useful for processing large datasets without loading everything into memory.
    /// </summary>
    /// <param name="connectionString">AdomdConnection connection string (e.g., "Data Source=localhost:55023;")</param>
    /// <param name="daxQuery">DAX query to execute (e.g., "EVALUATE 'TableName'")</param>
    /// <param name="commandTimeout">Command timeout in seconds (0 = no timeout, default: 0)</param>
    /// <returns>Enumerable of dictionaries where each dictionary represents a row with column names as keys</returns>
    public static IEnumerable<Dictionary<string, object?>> ExecuteDaxQueryEnumerable(
        string connectionString, 
        string daxQuery,
        int commandTimeout = 0)
    {
        using var connection = new AdomdConnection(connectionString);
        connection.Open();

        using var command = new AdomdCommand(daxQuery, connection);
        command.CommandTimeout = commandTimeout == 0 ? DaxSharpConfig.Instance.PowerBi.CommandTimeout : commandTimeout;

        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                // Clean column name: remove table prefix and square brackets
                var cleanedColumnName = DaxIdentifierHelper.CleanColumnName(columnName);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                // Store with cleaned name, but also keep original if different
                row[cleanedColumnName] = value;
                if (cleanedColumnName != columnName)
                {
                    row[columnName] = value; // Keep original for backward compatibility
                }
            }
            yield return row;
        }
    }


    /// <summary>
    /// Gets the complete database schema including tables, columns, and relationships from PowerBI.
    /// </summary>
    /// <param name="connectionString">AdomdConnection connection string</param>
    /// <returns>Database schema with tables, columns, and relationships</returns>
    public static DatabaseSchema GetDatabaseSchema(string connectionString)
    {
        var schema = new DatabaseSchema();
        
        using var connection = new AdomdConnection(connectionString);
        connection.Open();

        // Get all tables
        var tableNames = GetTables(connectionString);
        
        // Get columns for each table
        foreach (var tableName in tableNames)
        {
            var tableDesc = new Models.TableDescription { TableName = tableName };
            
            // Get columns using schema metadata
            try
            {
                var escapedTableName = DaxIdentifierHelper.EscapeDaxIdentifier(tableName);
                var config = DaxSharpConfig.Instance.PowerBi;
                var daxQuery = string.Format(config.DaxQueryTemplates.GetTableStructure, escapedTableName);
                
                using var command = new AdomdCommand(daxQuery, connection);
                command.CommandTimeout = config.CommandTimeout;
                
                using var reader = command.ExecuteReader();
                var schemaTable = reader.GetSchemaTable();
                
                if (schemaTable != null)
                {
                    foreach (DataRow row in schemaTable.Rows)
                    {
                        var columnName = row["ColumnName"]?.ToString() ?? string.Empty;
                        if (string.IsNullOrEmpty(columnName))
                            continue;
                        
                        // Clean column name
                        columnName = DaxIdentifierHelper.CleanColumnName(columnName);
                        
                        var dataType = row["DataType"]?.ToString() ?? "Unknown";
                        var isNullable = Convert.ToBoolean(row["AllowDBNull"]);
                        
                        tableDesc.Columns.Add(new Models.TableColumn
                        {
                            Name = columnName,
                            DataType = dataType,
                            IsNullable = isNullable
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting columns for table {tableName}: {ex.Message}");
            }
            
            schema.Tables.Add(tableDesc);
        }

        // Get relationships - try multiple methods (optional, failures are silently ignored)
        // Note: In PowerBI, relationships may not be accessible via system tables through ADOMD
        // This is not critical - schema will work without relationships
        bool relationshipsLoaded = false;
        
        // Method 1: Try using object model (most reliable)
        try
        {
            relationshipsLoaded = GetRelationshipsFromObjectModel(connection, schema);
        }
        catch
        {
            // Silently ignore - object model doesn't expose relationships in PowerBI
        }
        
        // Method 2: Try DMX query to TMSCHEMA_RELATIONSHIPS with different syntax variations
        if (!relationshipsLoaded)
        {
            var config = DaxSharpConfig.Instance.PowerBi;
            var queryVariations = new[]
            {
                "SELECT [FromTable], [FromColumn], [ToTable], [ToColumn], [FromCardinality], [ToCardinality] FROM $SYSTEM.TMSCHEMA_RELATIONSHIPS",
                "SELECT FromTable, FromColumn, ToTable, ToColumn, FromCardinality, ToCardinality FROM $SYSTEM.TMSCHEMA_RELATIONSHIPS",
                "SELECT * FROM $SYSTEM.TMSCHEMA_RELATIONSHIPS"
            };
            
            foreach (var relationshipsQuery in queryVariations)
            {
                try
                {
                    using var command = new AdomdCommand(relationshipsQuery, connection);
                    command.CommandTimeout = config.CommandTimeout;
                    command.CommandType = System.Data.CommandType.Text;
                
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var fromTable = DataReaderHelper.GetReaderValue(reader, "FromTable") ?? DataReaderHelper.GetReaderValue(reader, "FROMTABLE") ?? string.Empty;
                        var fromColumn = DataReaderHelper.GetReaderValue(reader, "FromColumn") ?? DataReaderHelper.GetReaderValue(reader, "FROMCOLUMN") ?? string.Empty;
                        var toTable = DataReaderHelper.GetReaderValue(reader, "ToTable") ?? DataReaderHelper.GetReaderValue(reader, "TOTABLE") ?? string.Empty;
                        var toColumn = DataReaderHelper.GetReaderValue(reader, "ToColumn") ?? DataReaderHelper.GetReaderValue(reader, "TOCOLUMN") ?? string.Empty;

                        if (string.IsNullOrEmpty(fromTable) || string.IsNullOrEmpty(toTable))
                        {
                            continue;
                        }

                        // Determine relationship type based on cardinality
                        var fromCardinality = DataReaderHelper.GetReaderValue(reader, "FromCardinality") ?? DataReaderHelper.GetReaderValue(reader, "FROMCARDINALITY") ?? string.Empty;
                        var toCardinality = DataReaderHelper.GetReaderValue(reader, "ToCardinality") ?? DataReaderHelper.GetReaderValue(reader, "TOCARDINALITY") ?? string.Empty;

                        var relationshipType = fromCardinality switch
                        {
                            "Many" when toCardinality == "One" => "ManyToOne",
                            "One" when toCardinality == "Many" => "OneToMany",
                            "One" when toCardinality == "One" => "OneToOne",
                            _ => "ManyToMany"
                        };

                        var relationship = new Models.TableRelationship
                        {
                            FromTable = fromTable,
                            FromColumn = DaxIdentifierHelper.CleanColumnName(fromColumn),
                            ToTable = toTable,
                            ToColumn = DaxIdentifierHelper.CleanColumnName(toColumn),
                            RelationshipType = relationshipType
                        };
                        
                        schema.AllRelationships.Add(relationship);
                        
                        // Add relationship to the source table
                        var sourceTable = schema.Tables.FirstOrDefault(t => t.TableName.Equals(fromTable, StringComparison.OrdinalIgnoreCase));
                        sourceTable?.Relationships.Add(relationship);
                    }
                    relationshipsLoaded = schema.AllRelationships.Count > 0;
                    if (relationshipsLoaded)
                        break; // Success, exit loop
                }
                catch
                {
                    // Silently try next variation - this is expected in PowerBI
                }
            }
        }
        
        // Method 3: Try alternative query format (also silently fails if not supported)
        if (relationshipsLoaded)
        {
            return schema;
        }

        try
        {
            var config = DaxSharpConfig.Instance.PowerBi;
            // Try using DISCOVER_XML_METADATA or alternative syntax
            const string altQuery = "SELECT [TABLE_NAME], [RELATED_TABLE_NAME], [FROM_COLUMN], [TO_COLUMN] FROM $SYSTEM.DISCOVER_SCHEMA_ROWSETS WHERE SchemaName='MDSCHEMA_RELATIONSHIPS'";

            using var command = new AdomdCommand(altQuery, connection);
            command.CommandTimeout = config.CommandTimeout;

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var fromTable = DataReaderHelper.GetReaderValue(reader, "TABLE_NAME") ?? string.Empty;
                var toTable = DataReaderHelper.GetReaderValue(reader, "RELATED_TABLE_NAME") ?? string.Empty;
                var fromColumn = DataReaderHelper.GetReaderValue(reader, "FROM_COLUMN") ?? string.Empty;
                var toColumn = DataReaderHelper.GetReaderValue(reader, "TO_COLUMN") ?? string.Empty;

                if (string.IsNullOrEmpty(fromTable) || string.IsNullOrEmpty(toTable))
                    continue;

                var relationship = new Models.TableRelationship
                {
                    FromTable = fromTable,
                    FromColumn = DaxIdentifierHelper.CleanColumnName(fromColumn),
                    ToTable = toTable,
                    ToColumn = DaxIdentifierHelper.CleanColumnName(toColumn),
                    RelationshipType = "ManyToOne" // Default assumption
                };

                schema.AllRelationships.Add(relationship);

                var sourceTable = schema.Tables.FirstOrDefault(t =>
                    t.TableName.Equals(fromTable, StringComparison.OrdinalIgnoreCase));
                sourceTable?.Relationships.Add(relationship);
            }
        }
        catch
        {
            // Silently ignore - system tables may not be accessible in PowerBI
        }
        

        // Relationships are optional - schema works fine without them
        // No warning needed - this is expected behavior in PowerBI

        return schema;
    }

    /// <summary>
    /// Gets a formatted string description of the database schema.
    /// </summary>
    /// <param name="connectionString">AdomdConnection connection string</param>
    /// <returns>Formatted string describing tables, columns, and relationships</returns>
    public static string GetDatabaseSchemaDescription(string connectionString)
    {
        var schema = GetDatabaseSchema(connectionString);
        var sb = new StringBuilder();
        
        foreach (var table in schema.Tables)
        {
            sb.AppendLine($"Table {table.TableName}, columns:");
            
            foreach (var column in table.Columns)
            {
                var nullableStr = column.IsNullable ? "nullable" : "not null";
                sb.AppendLine($"  {column.Name} ({column.DataType}, {nullableStr})");
            }
            
            if (table.Relationships.Count > 0)
            {
                sb.AppendLine("Relationships:");
                foreach (var rel in table.Relationships)
                {
                    sb.AppendLine($"  {table.TableName}[{rel.FromColumn}] -> {rel.ToTable}[{rel.ToColumn}]: {rel.RelationshipType}");
                }
            }
            
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Gets relationships from PowerBI using object model (most reliable method).
    /// </summary>
    private static bool GetRelationshipsFromObjectModel(AdomdConnection connection, Models.DatabaseSchema schema)
    {
        try
        {
            // Get the main cube
            CubeDef? mainCube = null;
            foreach (var cube in connection.Cubes)
            {
                if (cube.Name.StartsWith(DaxSharpConfig.Instance.PowerBi.SystemObjectPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                mainCube = cube;
                break;
            }

            if (mainCube == null)
            {
                return false;
            }

            // Try to get relationships from dimensions
            // In tabular models, relationships might be accessible through dimensions or measures
            // This is a fallback - relationships are typically not directly accessible via object model in PowerBI
            
            return false; // Object model doesn't directly expose relationships in PowerBI
        }
        catch
        {
            return false;
        }
    }
}
