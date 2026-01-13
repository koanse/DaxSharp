using System.Text.Json.Serialization;

namespace DaxSharp.Models;

/// <summary>
/// Result of DAX to SQL conversion and validation.
/// </summary>
public class DaxToSqlConversionResult
{
    /// <summary>
    /// Generated SQL query for PostgreSQL
    /// </summary>
    public string SqlQuery { get; set; } = string.Empty;
    
    /// <summary>
    /// Results from PowerBI (DAX query)
    /// </summary>
    public List<Dictionary<string, object?>> PowerBiResults { get; set; } = [];
    
    /// <summary>
    /// Results from PostgreSQL (SQL query)
    /// </summary>
    public List<Dictionary<string, object?>> PostgresResults { get; set; } = [];
    
    /// <summary>
    /// Whether the results match
    /// </summary>
    public bool ResultsMatch { get; set; }
    
    /// <summary>
    /// Number of iterations needed to generate correct SQL
    /// </summary>
    public int Iterations { get; set; }
    
    /// <summary>
    /// Error message if conversion failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Cache entry structure for storing request and response.
/// </summary>
internal class CacheEntry
{
    [JsonPropertyName("request")]
    public CacheRequest Request { get; init; } = new();
    
    [JsonPropertyName("response")]
    public DaxToSqlConversionResult Response { get; init; } = new();
}

/// <summary>
/// Request structure for cache key.
/// </summary>
internal class CacheRequest
{
    [JsonPropertyName("daxQuery")]
    public string DaxQuery { get; init; } = string.Empty;
    
    [JsonPropertyName("pbiConnectionString")]
    public string PbiConnectionString { get; init; } = string.Empty;
    
    [JsonPropertyName("postgresConnectionString")]
    public string PostgresConnectionString { get; init; } = string.Empty;
    
    [JsonPropertyName("schemaName")]
    public string SchemaName { get; init; } = "";
    
    [JsonIgnore]
    public string ApiKey { get; set; } = string.Empty;
    
    [JsonPropertyName("model")]
    public string Model { get; init; } = "";
    
    [JsonPropertyName("maxIterations")]
    public int MaxIterations { get; init; } = 0;
}

// Helper classes for OpenAI API response
internal class OpenAiResponse
{
    [JsonPropertyName("choices")]
    public OpenAiChoice[]? Choices { get; init; }
}

internal class OpenAiChoice
{
    [JsonPropertyName("message")]
    public OpenAiMessage? Message { get; set; }
}

internal class OpenAiMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
