namespace DaxSharp.Helpers;

/// <summary>
/// Helper methods for working with PostgreSQL identifiers.
/// </summary>
internal static class PostgresIdentifierHelper
{
    /// <summary>
    /// Escapes PostgreSQL identifier.
    /// </summary>
    public static string EscapeIdentifier(string identifier)
    {
        return string.IsNullOrEmpty(identifier) ? "\"\"" :
            // PostgreSQL identifiers are case-insensitive unless quoted
            // Quote to preserve case and handle special characters
            $"\"{identifier.Replace("\"", "\"\"")}\"";
    }
}
