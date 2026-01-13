namespace DaxSharp.Helpers;

/// <summary>
/// Helper methods for working with DAX identifiers.
/// </summary>
internal static class DaxIdentifierHelper
{
    /// <summary>
    /// Cleans column name by removing table prefix and square brackets.
    /// Converts "TableName[ColumnName]" or "TableName.ColumnName" to "ColumnName".
    /// </summary>
    public static string CleanColumnName(string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            return columnName;
        }

        // Remove square brackets format: TableName[ColumnName] -> ColumnName
        var bracketIndex = columnName.IndexOf('[');
        if (bracketIndex >= 0)
        {
            var closingBracket = columnName.LastIndexOf(']');
            if (closingBracket > bracketIndex)
            {
                return columnName.Substring(bracketIndex + 1, closingBracket - bracketIndex - 1);
            }
        }

        // Remove dot format: TableName.ColumnName -> ColumnName
        var dotIndex = columnName.LastIndexOf('.');
        if (dotIndex >= 0 && dotIndex < columnName.Length - 1)
        {
            return columnName.Substring(dotIndex + 1);
        }

        return columnName;
    }

    /// <summary>
    /// Helper method to escape DAX identifier (table/column name).
    /// In DAX, identifiers with spaces or special characters must be quoted with single quotes.
    /// Single quotes inside the identifier must be doubled.
    /// </summary>
    public static string EscapeDaxIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return "''";
        }

        // Check if identifier needs quoting (contains spaces, special chars, or starts with a number)
        var needsQuoting = identifier.Contains(' ') || 
                          identifier.Contains('-') || 
                          identifier.Contains('.') ||
                          identifier.Contains('[') ||
                          identifier.Contains(']') ||
                          char.IsDigit(identifier[0]) ||
                          identifier.Any(c => !char.IsLetterOrDigit(c) && c != '_');

        if (!needsQuoting)
        {
            return identifier;
        }

        // Escape single quotes by doubling them
        var escaped = identifier.Replace("'", "''");
        return $"'{escaped}'";
    }
}
