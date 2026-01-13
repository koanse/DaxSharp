using System.Text;
using Npgsql;

namespace DaxSharp.OpenAI;

/// <summary>
/// Formats SQL error messages for better understanding and iterative fixing.
/// </summary>
internal static class SqlErrorFormatter
{
    /// <summary>
    /// Formats SQL error message for better understanding and iterative fixing.
    /// </summary>
    public static string FormatSqlError(Exception ex, string sqlQuery)
    {
        var errorMsg = new StringBuilder();
        errorMsg.AppendLine($"SQL Error: {ex.Message}");
        
        // Extract PostgreSQL-specific error information if available
        if (ex is PostgresException pgEx)
        {
            errorMsg.AppendLine($"PostgreSQL Error Code: {pgEx.SqlState}");
            errorMsg.AppendLine($"Error Detail: {pgEx.Detail}");
            errorMsg.AppendLine($"Error Hint: {pgEx.Hint}");
            
            // Provide specific guidance based on error code
            switch (pgEx.SqlState)
            {
                case "42803": // Grouping error
                    errorMsg.AppendLine();
                    errorMsg.AppendLine("This is a GROUP BY error. The issue is:");
                    errorMsg.AppendLine("- A column is used in SELECT or HAVING that is not in GROUP BY");
                    errorMsg.AppendLine("- Or a column from outer query is used in a subquery without proper grouping");
                    errorMsg.AppendLine("Solution: Add all non-aggregated columns to GROUP BY, or use aggregate functions (SUM, COUNT, etc.) for those columns.");
                    break;
                case "42P01": // Undefined table
                    errorMsg.AppendLine();
                    errorMsg.AppendLine("Table or view does not exist. Check table names and schema.");
                    break;
                case "42703": // Undefined column
                    errorMsg.AppendLine();
                    errorMsg.AppendLine("Column does not exist. Check column names and table aliases.");
                    break;
                case "42601": // Syntax error
                    errorMsg.AppendLine();
                    errorMsg.AppendLine("SQL syntax error. Check the query structure, parentheses, and SQL keywords.");
                    break;
            }
            
            if (pgEx.Position > 0)
            {
                errorMsg.AppendLine();
                errorMsg.AppendLine($"Error position in SQL: {pgEx.Position}");
                // Show context around the error position
                if (pgEx.Position <= sqlQuery.Length)
                {
                    var start = Math.Max(0, pgEx.Position - 50);
                    var length = Math.Min(100, sqlQuery.Length - start);
                    var context = sqlQuery.Substring(start, length);
                    errorMsg.AppendLine($"SQL context around error: ...{context}...");
                }
            }
        }
        else
        {
            errorMsg.AppendLine($"Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                errorMsg.AppendLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
        
        return errorMsg.ToString();
    }
}
