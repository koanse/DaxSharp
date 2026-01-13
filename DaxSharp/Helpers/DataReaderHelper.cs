using System.Data;

namespace DaxSharp.Helpers;

/// <summary>
/// Helper methods for working with data readers.
/// </summary>
internal static class DataReaderHelper
{
    /// <summary>
    /// Helper method to get value from data reader with case-insensitive column name matching.
    /// </summary>
    public static string? GetReaderValue(IDataReader reader, string columnName)
    {
        try
        {
            // Try exact match first
            if (reader.GetOrdinal(columnName) >= 0)
            {
                var value = reader[columnName];
                return value?.ToString();
            }
        }
        catch
        {
            // Try case-insensitive match
            try
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        var value = reader[i];
                        return value?.ToString();
                    }
                }
            }
            catch
            {
                // Column not found
            }
        }
        return null;
    }
}
