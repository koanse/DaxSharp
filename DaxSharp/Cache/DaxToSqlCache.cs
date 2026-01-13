using System.Text.Json;
using System.Text.Json.Serialization;
using DaxSharp.Models;

namespace DaxSharp.Cache;

/// <summary>
/// Manages cache for DAX to SQL conversion results.
/// </summary>
internal static class DaxToSqlCache
{
    /// <summary>
    /// Gets the cache file path. 
    /// Tries to use the executable directory, falls back to current directory.
    /// </summary>
    private static string GetCacheFilePath()
    {
        // Try to use the directory where the executable is located
        // This is more predictable than current working directory
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var config = DaxSharpConfig.Instance;
        return Path.Combine(!string.IsNullOrEmpty(baseDirectory)
            ? baseDirectory
            : Directory.GetCurrentDirectory(), config.Cache.FileName);
    }

    /// <summary>
    /// Loads cache entries from JSON file.
    /// </summary>
    private static List<CacheEntry> LoadCacheEntries()
    {
        var cacheFilePath = GetCacheFilePath();
        
        if (!File.Exists(cacheFilePath))
        {
            return [];
        }

        try
        {
            var jsonContent = File.ReadAllText(cacheFilePath);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return [];
            }

            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var entries = JsonSerializer.Deserialize<List<CacheEntry>>(jsonContent, jsonOptions);
            return entries ?? [];
        }
        catch
        {
            // If cache file is corrupted, return empty list
            return [];
        }
    }

    /// <summary>
    /// Saves cache entries to JSON file.
    /// </summary>
    private static void SaveCacheEntries(List<CacheEntry> entries)
    {
        var cacheFilePath = GetCacheFilePath();
        
        // Log cache file location on first save (optional, can be removed if not needed)
        if (!File.Exists(cacheFilePath) && entries.Count > 0)
        {
            Console.WriteLine($"Cache file will be created at: {cacheFilePath}");
        }
        
        var jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
        
        var jsonContent = JsonSerializer.Serialize(entries, jsonOptions);
        File.WriteAllText(cacheFilePath, jsonContent);
    }

    /// <summary>
    /// Compares two cache requests for equality.
    /// Note: ApiKey is not included in comparison for security reasons.
    /// </summary>
    private static bool CacheRequestsEqual(CacheRequest request1, CacheRequest request2)
    {
        return request1.DaxQuery == request2.DaxQuery &&
               request1.PbiConnectionString == request2.PbiConnectionString &&
               request1.PostgresConnectionString == request2.PostgresConnectionString &&
               request1.SchemaName == request2.SchemaName &&
               request1.Model == request2.Model &&
               request1.MaxIterations == request2.MaxIterations;
    }

    /// <summary>
    /// Loads result from cache if matching request is found.
    /// </summary>
    public static DaxToSqlConversionResult? LoadFromCache(CacheRequest request)
    {
        var entries = LoadCacheEntries();
        
        var matchingEntry = entries.FirstOrDefault(e => CacheRequestsEqual(e.Request, request));
        
        return matchingEntry?.Response;
    }

    /// <summary>
    /// Saves result to cache.
    /// </summary>
    public static void SaveToCache(CacheRequest request, DaxToSqlConversionResult response)
    {
        var entries = LoadCacheEntries();
        
        // Remove existing entry with the same request if exists
        entries.RemoveAll(e => CacheRequestsEqual(e.Request, request));
        
        // Add new entry
        entries.Add(new CacheEntry
        {
            Request = request,
            Response = response
        });
        
        // Save to file
        SaveCacheEntries(entries);
    }
}
