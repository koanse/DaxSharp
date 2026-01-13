using System.Text;

namespace DaxSharp.Comparison;

/// <summary>
/// Compares results from PowerBI and PostgreSQL.
/// </summary>
internal static class ResultComparer
{
    /// <summary>
    /// Compares results from PowerBI and PostgreSQL.
    /// </summary>
    public static (bool Match, string DifferenceDescription) CompareResults(
        List<Dictionary<string, object?>> powerBiResults,
        List<Dictionary<string, object?>> postgresResults)
    {
        // Normalize and sort results for comparison
        var normalizedPbi = NormalizeResults(powerBiResults);
        var normalizedPg = NormalizeResults(postgresResults);
        
        // Debug: Log original column names for troubleshooting
        var originalPbiKeys = powerBiResults.FirstOrDefault()?.Keys.ToList() ?? [];
        var originalPgKeys = postgresResults.FirstOrDefault()?.Keys.ToList() ?? [];
        
        if (normalizedPbi.Count != normalizedPg.Count)
        {
            // Check for duplicate rows
            var pbiUniqueRows = normalizedPbi.Distinct().Count();
            var pgUniqueRows = normalizedPg.Distinct().Count();
            
            var errorMsg = $"Row count mismatch: PowerBI returned {normalizedPbi.Count} rows, PostgreSQL returned {normalizedPg.Count} rows";
            
            if (pbiUniqueRows != normalizedPbi.Count)
            {
                errorMsg += $"\nPowerBI has {normalizedPbi.Count - pbiUniqueRows} duplicate rows (unique: {pbiUniqueRows})";
            }
            
            if (pgUniqueRows != normalizedPg.Count)
            {
                errorMsg += $"\nPostgreSQL has {normalizedPg.Count - pgUniqueRows} duplicate rows (unique: {pgUniqueRows})";
            }
            
            // Show sample data from both results
            if (normalizedPbi.Count > 0 && normalizedPg.Count > 0)
            {
                errorMsg += $"\n\nPowerBI first row keys: [{string.Join(", ", normalizedPbi[0].Keys.OrderBy(k => k))}]";
                errorMsg += $"\nPostgreSQL first row keys: [{string.Join(", ", normalizedPg[0].Keys.OrderBy(k => k))}]";
                
                // Show first few rows to help identify the pattern
                errorMsg += $"\n\nPowerBI first 3 rows:";
                for (var i = 0; i < Math.Min(3, normalizedPbi.Count); i++)
                {
                    var row = normalizedPbi[i];
                    var rowStr = string.Join(", ", row.Keys.OrderBy(k => k).Select(k => $"{k}={row[k]}"));
                    errorMsg += $"\n  Row {i + 1}: {rowStr}";
                }
                
                errorMsg += $"\n\nPostgreSQL first 5 rows:";
                for (var i = 0; i < Math.Min(5, normalizedPg.Count); i++)
                {
                    var row = normalizedPg[i];
                    var rowStr = string.Join(", ", row.Keys.OrderBy(k => k).Select(k => $"{k}={row[k]}"));
                    errorMsg += $"\n  Row {i + 1}: {rowStr}";
                }
            }
            
            // Provide suggestions based on the issue
            if (normalizedPg.Count > normalizedPbi.Count)
            {
                errorMsg += $"\n\nThe SQL query is returning too many rows ({normalizedPg.Count} vs {normalizedPbi.Count} expected).";
                
                // Analyze if there are duplicate grouping keys
                var pbiGroupKeys = normalizedPbi.Select(r => 
                    string.Join("|", r.Keys.Where(k => !k.Contains("measure", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(k => k).Select(k => $"{k}={r[k]}"))).ToList();
                var pgGroupKeys = normalizedPg.Select(r => 
                    string.Join("|", r.Keys.Where(k => !k.Contains("measure", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(k => k).Select(k => $"{k}={r[k]}"))).ToList();
                
                var duplicateKeys = pgGroupKeys.GroupBy(k => k)
                    .Where(g => g.Count() > 1)
                    .Select(g => new { Key = g.Key, Count = g.Count() })
                    .ToList();
                
                if (duplicateKeys.Any())
                {
                    errorMsg += $"\n\nCRITICAL: Found {duplicateKeys.Count} duplicate grouping key(s) in PostgreSQL results:";
                    foreach (var dup in duplicateKeys.Take(3))
                    {
                        errorMsg += $"\n- Key '{dup.Key}' appears {dup.Count} times (should appear only once)";
                    }
                    errorMsg += $"\n\nThis indicates missing or incorrect GROUP BY clause. All non-aggregated columns must be in GROUP BY.";
                    errorMsg += $"\nExample: If grouping by 'colorname', use: GROUP BY colorname";
                }
                
                errorMsg += $"\n\nPossible causes:";
                errorMsg += $"\n- Missing or incorrect GROUP BY clause (most likely - check grouping columns)";
                errorMsg += $"\n- Missing aggregate functions (SUM, COUNT, etc.) for measure columns";
                errorMsg += $"\n- Incorrect JOIN conditions causing row multiplication";
                errorMsg += $"\n- Missing DISTINCT where needed";
                errorMsg += $"\n- Incorrect filtering or WHERE conditions";
            }
            else
            {
                errorMsg += $"\n\nThe SQL query is returning too few rows. Possible causes:";
                errorMsg += $"\n- Too restrictive WHERE conditions";
                errorMsg += $"\n- Incorrect JOIN type (should be LEFT JOIN instead of INNER JOIN)";
                errorMsg += $"\n- Missing rows due to NULL handling";
            }
            
            return (false, errorMsg);
        }
        
        // Compare each row
        for (var i = 0; i < normalizedPbi.Count; i++)
        {
            var pbiRow = normalizedPbi[i];
            var pgRow = normalizedPg[i];
            
            if (pbiRow.Count != pgRow.Count)
            {
                return (false, $"Row {i + 1}: Column count mismatch. PowerBI: {pbiRow.Count}, PostgreSQL: {pgRow.Count}");
            }
            
            foreach (var kvp in pbiRow)
            {
                // Keys are already normalized to lowercase in NormalizeResults
                // But use case-insensitive lookup to be safe
                var matchingKey = pgRow.Keys.FirstOrDefault(k => 
                    k.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                
                if (matchingKey == null)
                {
                    // Provide detailed error message with available columns
                    // Show both normalized keys and original keys for debugging
                    var similarKeys = pgRow.Keys.Where(k => 
                        k.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) || 
                        kvp.Key.Contains(k, StringComparison.OrdinalIgnoreCase)).ToList();
                    
                    var errorMsg = $"Row {i + 1}: Column '{kvp.Key}' (normalized) missing in PostgreSQL results.\n" +
                                  $"PowerBI columns (normalized): [{string.Join(", ", pbiRow.Keys.OrderBy(k => k))}]\n" +
                                  $"PostgreSQL columns (normalized): [{string.Join(", ", pgRow.Keys.OrderBy(k => k))}]\n" +
                                  $"PowerBI columns (original): [{string.Join(", ", originalPbiKeys)}]\n" +
                                  $"PostgreSQL columns (original): [{string.Join(", ", originalPgKeys)}]";
                    
                    if (similarKeys.Any())
                    {
                        errorMsg += $"\nSimilar keys found: [{string.Join(", ", similarKeys)}]";
                    }
                    
                    return (false, errorMsg);
                }
                
                var pbiValue = NormalizeValue(kvp.Value);
                var pgValue = NormalizeValue(pgRow[matchingKey]);
                
                if (!ValuesEqual(pbiValue, pgValue))
                {
                    // Calculate difference for numeric values to help with debugging
                    var differenceInfo = string.Empty;
                    if (pbiValue is decimal pbiDec && pgValue is decimal pgDec)
                    {
                        var diff = pgDec - pbiDec;
                        var diffPercent = pbiDec != 0 ? (diff / Math.Abs(pbiDec)) * 100 : 0;
                        differenceInfo = $" (difference: {diff:F2}, {diffPercent:F1}%)";
                    }
                    
                    // Build context about the row to help with debugging
                    var rowContext = new StringBuilder();
                    rowContext.Append($"Row {i + 1} details: ");
                    foreach (var key in pbiRow.Keys.OrderBy(k => k))
                    {
                        var pbiVal = pbiRow[key];
                        var pgVal = pgRow.ContainsKey(key) ? pgRow[key] : "N/A";
                        rowContext.Append($"{key}=PBI:{pbiVal}/PG:{pgVal}; ");
                    }
                    
                    return (false, $"Row {i + 1}, Column '{kvp.Key}': PowerBI value = {pbiValue}, PostgreSQL value = {pgValue}{differenceInfo}. " +
                            $"The SQL query is returning incorrect values for this column. Please check the calculation logic, aggregations, JOIN conditions, and filters. " +
                            $"{rowContext}");
                }
            }
        }
        
        return (true, string.Empty);
    }

    /// <summary>
    /// Normalizes column name for comparison: removes quotes, trims whitespace, converts to lowercase.
    /// </summary>
    private static string NormalizeColumnName(string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return columnName;
        
        // Remove quotes (single and double)
        var normalized = columnName.Replace("\"", "").Replace("'", "");
        // Trim whitespace
        normalized = normalized.Trim();
        // Convert to lowercase
        normalized = normalized.ToLowerInvariant();
        
        return normalized;
    }

    /// <summary>
    /// Normalizes results by converting to comparable format and sorting.
    /// Normalizes column names to lowercase for case-insensitive comparison.
    /// </summary>
    private static List<Dictionary<string, string>> NormalizeResults(
        List<Dictionary<string, object?>> results)
    {
        var normalized = results.Select(row =>
        {
            var normalizedRow = new Dictionary<string, string>();
            foreach (var kvp in row.OrderBy(k => k.Key))
            {
                // Normalize column name: remove quotes, trim whitespace and convert to lowercase
                var normalizedKey = NormalizeColumnName(kvp.Key);
                normalizedRow[normalizedKey] = NormalizeValue(kvp.Value)?.ToString() ?? "NULL";
            }
            return normalizedRow;
        }).ToList();
        
        // Sort by all column values for consistent comparison
        normalized.Sort((a, b) =>
        {
            foreach (var key in a.Keys.OrderBy(k => k))
            {
                var aVal = a[key] ?? "NULL";
                var bVal = b[key] ?? "NULL";
                var comparison = string.Compare(aVal, bVal, StringComparison.OrdinalIgnoreCase);
                if (comparison != 0)
                    return comparison;
            }
            return 0;
        });
        
        return normalized;
    }

    /// <summary>
    /// Normalizes a value for comparison (handles different numeric types, dates, etc.).
    /// </summary>
    private static object? NormalizeValue(object? value)
    {
        if (value == null || value == DBNull.Value)
        {
            return null;
        }

        return value switch
        {
            // Convert numeric types to decimal for comparison
            byte or short or int or long or float or double or decimal => Convert.ToDecimal(value),
            // Convert DateTime to string for comparison
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"),
            _ => value
        };
    }

    /// <summary>
    /// Compares two normalized values for equality.
    /// </summary>
    private static bool ValuesEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null)
        {
            return true;
        }

        if (value1 == null || value2 == null)
        {
            return false;
        }

        // For decimal comparison, use tolerance
        if (value1 is decimal d1 && value2 is decimal d2)
        {
            return Math.Abs(d1 - d2) < 0.0001m;
        }
        
        return value1.Equals(value2);
    }
}
