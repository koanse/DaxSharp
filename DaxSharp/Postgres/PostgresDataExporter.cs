using System.Data;
using Npgsql;
using DaxSharp.Helpers;

namespace DaxSharp.Postgres;

/// <summary>
/// Exports data from PowerBI to PostgreSQL.
/// </summary>
internal static class PostgresDataExporter
{
    /// <summary>
    /// Exports data from PowerBI table to PostgreSQL.
    /// </summary>
    public static int ExportTableData(
        string pbiConnectionString,
        string postgresConnectionString,
        string schemaName,
        string tableName,
        DataTable tableStructure)
    {
        var escapedTableName = PostgresIdentifierHelper.EscapeIdentifier(tableName);
        var escapedSchemaName = PostgresIdentifierHelper.EscapeIdentifier(schemaName);
        var fullTableName = $"{escapedSchemaName}.{escapedTableName}";

        // Get column names
        var columnNames = tableStructure.Columns.Cast<DataColumn>()
            .Select(c => PostgresIdentifierHelper.EscapeIdentifier(c.ColumnName))
            .ToArray();

        // Build INSERT statement
        var config = DaxSharpConfig.Instance;
        var columnNamesStr = string.Join(", ", columnNames);
        var placeholders = string.Join(", ", columnNames.Select((_, i) => $"${i + 1}"));
        var insertSql = string.Format(config.PostgreSql.SqlTemplates.InsertInto, fullTableName, columnNamesStr, placeholders);

        var rowCount = 0;

        // Stream data from PBI and insert into PostgreSQL
        using var postgresConnection = new NpgsqlConnection(postgresConnectionString);
        postgresConnection.Open();

        // Escape single quotes in table name for DAX (double them)
        var daxTableName = DaxIdentifierHelper.EscapeDaxIdentifier(tableName);
        var daxQuery = string.Format(config.PowerBi.DaxQueryTemplates.GetAllData, daxTableName);
        var rows = DaxSharpPbiExportExtensions.ExecuteDaxQuery(pbiConnectionString, daxQuery);

        // Use batch insert for better performance
        var batchSize = config.PostgreSql.BatchSize;
        var batch = new List<Dictionary<string, object?>>();

        foreach (var row in rows)
        {
            batch.Add(row);

            if (batch.Count < batchSize)
            {
                continue;
            }

            InsertBatch(postgresConnection, insertSql, columnNames, batch);
            rowCount += batch.Count;
            batch.Clear();
        }

        // Insert remaining rows
        if (batch.Count <= 0)
        {
            return rowCount;
        }

        InsertBatch(postgresConnection, insertSql, columnNames, batch);
        rowCount += batch.Count;

        return rowCount;
    }

    /// <summary>
    /// Inserts a batch of rows into PostgreSQL.
    /// </summary>
    private static void InsertBatch(
        NpgsqlConnection connection,
        string insertSql,
        string[] columnNames,
        List<Dictionary<string, object?>> batch)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            foreach (var row in batch)
            {
                using var command = new NpgsqlCommand(insertSql, connection, transaction);
                
                foreach (var t in columnNames)
                {
                    var columnName = t.Trim('"');
                    object? value = null;
                    
                    // Try to get value by exact column name match
                    if (row.TryGetValue(columnName, out var value1))
                    {
                        value = value1;
                    }
                    else
                    {
                        // Try case-insensitive match with cleaned name
                        var matchingKey = row.Keys.FirstOrDefault(k => 
                        {
                            var cleanedKey = DaxIdentifierHelper.CleanColumnName(k);
                            return cleanedKey.Equals(columnName, StringComparison.OrdinalIgnoreCase);
                        });
                        if (matchingKey != null)
                        {
                            value = row[matchingKey];
                        }
                        else
                        {
                            // Try exact match with original key (might have table prefix)
                            matchingKey = row.Keys.FirstOrDefault(k => 
                                k.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                            if (matchingKey != null)
                            {
                                value = row[matchingKey];
                            }
                        }
                    }
                    
                    // Use positional parameters ($1, $2, etc.)
                    var parameter = new NpgsqlParameter
                    {
                        Value = value ?? DBNull.Value
                    };
                    command.Parameters.Add(parameter);
                }

                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
