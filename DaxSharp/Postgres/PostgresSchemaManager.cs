using System.Data;
using Npgsql;
using DaxSharp.Helpers;

namespace DaxSharp.Postgres;

/// <summary>
/// Manages PostgreSQL schema operations.
/// </summary>
internal static class PostgresSchemaManager
{
    /// <summary>
    /// Ensures that the specified schema exists in PostgreSQL.
    /// </summary>
    public static void EnsureSchema(NpgsqlConnection connection, string schemaName)
    {
        var escapedSchemaName = PostgresIdentifierHelper.EscapeIdentifier(schemaName);
        var config = DaxSharpConfig.Instance;
        var createSchemaSql = string.Format(config.PostgreSql.SqlTemplates.CreateSchema, escapedSchemaName);
        
        using var command = new NpgsqlCommand(createSchemaSql, connection);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Creates a table in PostgreSQL based on the structure from PowerBI.
    /// </summary>
    public static void CreatePostgresTable(
        string postgresConnectionString,
        string schemaName,
        string tableName,
        DataTable tableStructure)
    {
        using var connection = new NpgsqlConnection(postgresConnectionString);
        connection.Open();

        var escapedTableName = PostgresIdentifierHelper.EscapeIdentifier(tableName);
        var escapedSchemaName = PostgresIdentifierHelper.EscapeIdentifier(schemaName);
        var fullTableName = $"{escapedSchemaName}.{escapedTableName}";

        // Always drop table first to avoid constraint conflicts
        // This ensures a clean table creation without existing constraints
        var config = DaxSharpConfig.Instance;
        var dropTableSql = string.Format(config.PostgreSql.SqlTemplates.DropTable, fullTableName);
        using (var dropTableCommand = new NpgsqlCommand(dropTableSql, connection))
        {
            dropTableCommand.ExecuteNonQuery();
        }

        // Build CREATE TABLE statement
        var columns = new List<string>();
        foreach (DataColumn column in tableStructure.Columns)
        {
            var escapedColumnName = PostgresIdentifierHelper.EscapeIdentifier(column.ColumnName);
            var postgresType = PostgresTypeMapper.MapToPostgresType(column.DataType);
            // Always allow NULL since we only check the first row structure
            // and other rows might have NULL values
            columns.Add($"{escapedColumnName} {postgresType} NULL");
        }

        var columnsStr = string.Join(", ", columns);
        var createTableSql = string.Format(config.PostgreSql.SqlTemplates.CreateTable, fullTableName, columnsStr);

        using var createCommand = new NpgsqlCommand(createTableSql, connection);
        createCommand.ExecuteNonQuery();
    }
}
