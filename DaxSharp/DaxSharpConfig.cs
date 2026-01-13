using System.Text.Json;
using System.Text.Json.Serialization;

namespace DaxSharp;

/// <summary>
/// Wrapper class for DaxSharp configuration section in appsettings.json.
/// </summary>
internal class DaxSharpConfigWrapper
{
    [JsonPropertyName("DaxSharp")]
    public DaxSharpConfig? DaxSharp { get; set; }
}

/// <summary>
/// Configuration class for DaxSharp constants loaded from JSON file.
/// </summary>
public class DaxSharpConfig
{
    private static DaxSharpConfig? _instance;
    private static readonly object _lock = new object();

    [JsonPropertyName("PowerBi")]
    public PowerBiConfig PowerBi { get; set; } = new();

    [JsonPropertyName("PostgreSQL")]
    public PostgreSqlConfig PostgreSql { get; set; } = new();

    [JsonPropertyName("OpenAI")]
    public OpenAiConfig OpenAi { get; set; } = new();

    [JsonPropertyName("Cache")]
    public CacheConfig Cache { get; set; } = new();

    [JsonPropertyName("ErrorMessages")]
    public ErrorMessagesConfig ErrorMessages { get; set; } = new();

    [JsonPropertyName("ConnectionStrings")]
    public ConnectionStringsConfig ConnectionStrings { get; set; } = new();

    /// <summary>
    /// Gets the singleton instance of the configuration.
    /// </summary>
    public static DaxSharpConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = LoadConfig();
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Loads configuration from JSON file.
    /// </summary>
    private static DaxSharpConfig LoadConfig()
    {
        var configPath = GetConfigFilePath();
        
        if (!File.Exists(configPath))
        {
            // Return default configuration if file doesn't exist
            return new DaxSharpConfig();
        }

        try
        {
            var jsonContent = File.ReadAllText(configPath);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return new DaxSharpConfig();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // First, try to deserialize as DaxSharpConfigWrapper to get the DaxSharp section
            var wrapper = JsonSerializer.Deserialize<DaxSharpConfigWrapper>(jsonContent, options);
            if (wrapper?.DaxSharp != null)
            {
                return wrapper.DaxSharp;
            }

            // Fallback: try to deserialize directly (for backward compatibility)
            var config = JsonSerializer.Deserialize<DaxSharpConfig>(jsonContent, options);
            return config ?? new DaxSharpConfig();
        }
        catch
        {
            // Return default configuration if loading fails
            return new DaxSharpConfig();
        }
    }

    /// <summary>
    /// Gets the configuration file path.
    /// Searches in multiple locations: current directory, base directory, and parent directories.
    /// </summary>
    private static string GetConfigFilePath()
    {
        var searchPaths = new List<string>();
        
        // 1. Base directory (where executable is located)
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        if (!string.IsNullOrEmpty(baseDirectory))
        {
            searchPaths.Add(baseDirectory);
        }
        
        // 2. Current working directory
        var currentDirectory = Directory.GetCurrentDirectory();
        if (!string.IsNullOrEmpty(currentDirectory) && !searchPaths.Contains(currentDirectory))
        {
            searchPaths.Add(currentDirectory);
        }
        
        // 3. Assembly directory and parent directories (for test projects)
        var assemblyLocation = typeof(DaxSharpConfig).Assembly.Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(assemblyDirectory) && !searchPaths.Contains(assemblyDirectory))
            {
                searchPaths.Add(assemblyDirectory);
                
                // Check parent directories up to 4 levels (to find project root)
                var parentDir = Directory.GetParent(assemblyDirectory);
                for (int i = 0; i < 4 && parentDir != null; i++)
                {
                    if (!searchPaths.Contains(parentDir.FullName))
                    {
                        searchPaths.Add(parentDir.FullName);
                    }
                    parentDir = parentDir.Parent;
                }
            }
        }
        
        // 4. Also check in the DaxSharp project directory (for tests that reference the library)
        var currentDir = Directory.GetCurrentDirectory();
        if (!string.IsNullOrEmpty(currentDir))
        {
            // Try to find DaxSharp project directory
            var dir = new DirectoryInfo(currentDir);
            while (dir != null)
            {
                var daxSharpDir = Path.Combine(dir.FullName, "DaxSharp", "DaxSharp");
                if (Directory.Exists(daxSharpDir) && !searchPaths.Contains(daxSharpDir))
                {
                    searchPaths.Add(daxSharpDir);
                }
                dir = dir.Parent;
            }
        }
        
        // Search for appsettings.json in all paths
        foreach (var configPath in searchPaths.Select(path => Path.Combine(path, "appsettings.json")).Where(File.Exists))
        {
            return configPath;
        }
        
        // If not found, return path in base directory (will be created if needed)
        return Path.Combine(baseDirectory ?? currentDirectory, "appsettings.json");
    }
}

public class PowerBiConfig
{
    [JsonPropertyName("SystemObjectPrefix")]
    public string SystemObjectPrefix { get; set; } = "$";

    [JsonPropertyName("ModelTableName")]
    public string ModelTableName { get; set; } = "Model";

    [JsonPropertyName("MeasuresTableName")]
    public string MeasuresTableName { get; set; } = "Measures";

    [JsonPropertyName("CommandTimeout")]
    public int CommandTimeout { get; set; } = 0;

    [JsonPropertyName("DaxQueryTemplates")]
    public DaxQueryTemplatesConfig DaxQueryTemplates { get; set; } = new();

    [JsonPropertyName("RelationshipsQueries")]
    public RelationshipsQueriesConfig RelationshipsQueries { get; set; } = new();
}

public class DaxQueryTemplatesConfig
{
    [JsonPropertyName("GetTableStructure")]
    public string GetTableStructure { get; set; } = "EVALUATE TOPN(1, {0})";

    [JsonPropertyName("GetAllData")]
    public string GetAllData { get; set; } = "EVALUATE {0}";
}

public class RelationshipsQueriesConfig
{
    [JsonPropertyName("Primary")]
    public string Primary { get; set; } = "SELECT FromTable, FromColumn, ToTable, ToColumn, FromCardinality, ToCardinality FROM $SYSTEM.TMSCHEMA_RELATIONSHIPS";

    [JsonPropertyName("Alternative")]
    public string Alternative { get; set; } = "SELECT TABLE_NAME, RELATED_TABLE_NAME, FROM_COLUMN, TO_COLUMN FROM $SYSTEM.MDSCHEMA_RELATIONSHIPS";
}

public class PostgreSqlConfig
{
    [JsonPropertyName("DefaultSchema")]
    public string DefaultSchema { get; set; } = "public";

    [JsonPropertyName("BatchSize")]
    public int BatchSize { get; set; } = 1000;

    [JsonPropertyName("SqlTemplates")]
    public SqlTemplatesConfig SqlTemplates { get; set; } = new();

    [JsonPropertyName("TypeMappings")]
    public Dictionary<string, string> TypeMappings { get; set; } = new();
}

public class SqlTemplatesConfig
{
    [JsonPropertyName("DropTable")]
    public string DropTable { get; set; } = "DROP TABLE IF EXISTS {0} CASCADE;";

    [JsonPropertyName("CreateTable")]
    public string CreateTable { get; set; } = "CREATE TABLE {0} ({1});";

    [JsonPropertyName("CreateSchema")]
    public string CreateSchema { get; set; } = "CREATE SCHEMA IF NOT EXISTS {0};";

    [JsonPropertyName("InsertInto")]
    public string InsertInto { get; set; } = "INSERT INTO {0} ({1}) VALUES ({2})";
}

public class OpenAiConfig
{
    [JsonPropertyName("ApiUrl")]
    public string ApiUrl { get; set; } = "https://api.openai.com/v1/chat/completions";

    [JsonPropertyName("DefaultModel")]
    public string DefaultModel { get; set; } = "gpt-4";

    [JsonPropertyName("DefaultApiKey")]
    public string DefaultApiKey { get; set; } = "";

    [JsonPropertyName("DefaultMaxIterations")]
    public int DefaultMaxIterations { get; set; } = 3;

    [JsonPropertyName("Temperature")]
    public double Temperature { get; set; } = 0.3;

    [JsonPropertyName("MaxTokens")]
    public int MaxTokens { get; set; } = 2000;

    [JsonPropertyName("Prompts")]
    public PromptsConfig Prompts { get; set; } = new();
}

public class PromptsConfig
{
    [JsonPropertyName("InitialConversion")]
    public string InitialConversion { get; set; } = "Convert the following DAX query to a PostgreSQL SQL query.\n\nDatabase Schema:\n{0}\n\nDAX Query:\n{1}\n\nPostgreSQL Schema Name: {2}\n\nGenerate a valid PostgreSQL SQL query that produces the same results as the DAX query.\nUse the provided database schema information to correctly join tables and reference columns.\nReturn only the SQL query without any explanations or markdown formatting.";

    [JsonPropertyName("FixQuery")]
    public string FixQuery { get; set; } = "The previous SQL query had issues. Please fix it.\n\nDatabase Schema:\n{0}\n\nOriginal DAX Query:\n{1}\n\nPrevious SQL Query:\n{2}\n\nError or Issue:\n{3}\n\nPostgreSQL Schema Name: {4}\n\n{5}Generate a corrected PostgreSQL SQL query. Use the provided database schema information to correctly join tables and reference columns.\nEnsure proper GROUP BY and aggregate functions are used.\nReturn only the SQL query without any explanations or markdown formatting.";

    [JsonPropertyName("GroupByGuidance")]
    public string GroupByGuidance { get; set; } = "IMPORTANT: The SQL query has GROUP BY issues. Follow these rules:\n1. All columns in SELECT that are not aggregate functions MUST be in GROUP BY\n2. All measure columns (like measure1, measure2, measure3) MUST use aggregate functions (SUM, COUNT, AVG, etc.)\n3. Example: SELECT colorname, SUM(measure2) AS measure2, SUM(measure3) AS measure3 FROM ... GROUP BY colorname\n4. Make sure to group by all non-aggregated columns from the SELECT clause\n\n";

    [JsonPropertyName("TooManyRowsGuidance")]
    public string TooManyRowsGuidance { get; set; } = "IMPORTANT: The SQL query returns too many rows. This usually means:\n1. Missing GROUP BY clause - add GROUP BY for all non-aggregated columns\n2. Missing aggregate functions - use SUM(), COUNT(), etc. for measure columns\n3. Incorrect JOINs causing row multiplication - check JOIN conditions\n\n";
}

public class CacheConfig
{
    [JsonPropertyName("FileName")]
    public string FileName { get; set; } = "dax_to_sql_cache.json";
}

public class ErrorMessagesConfig
{
    [JsonPropertyName("DaxQueryNoResults")]
    public string DaxQueryNoResults { get; set; } = "DAX query returned no results";

    [JsonPropertyName("FailedToGenerateSql")]
    public string FailedToGenerateSql { get; set; } = "Failed to generate SQL from OpenAI";
}

public class ConnectionStringsConfig
{
    [JsonPropertyName("PowerBi")]
    public string PowerBi { get; set; } = "";

    [JsonPropertyName("PostgreSQL")]
    public string PostgreSQL { get; set; } = "";
}
