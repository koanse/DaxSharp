using Npgsql;

namespace DaxSharp.Postgres;

/// <summary>
/// Executes SQL queries in PostgreSQL.
/// </summary>
internal static class PostgresQueryExecutor
{
    /// <summary>
    /// Executes SQL query in PostgreSQL and returns results.
    /// </summary>
    public static List<Dictionary<string, object?>> ExecutePostgresQuery(
        string postgresConnectionString,
        string sqlQuery)
    {
        var results = new List<Dictionary<string, object?>>();
        
        using var connection = new NpgsqlConnection(postgresConnectionString);
        connection.Open();
        
        using var command = new NpgsqlCommand(sqlQuery, connection);
        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[columnName] = value;
            }
            results.Add(row);
        }
        
        return results;
    }
}
