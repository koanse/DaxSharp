namespace DaxSharp.Postgres;

/// <summary>
/// Maps .NET types to PostgreSQL types.
/// </summary>
internal static class PostgresTypeMapper
{
    /// <summary>
    /// Maps .NET types to PostgreSQL types.
    /// </summary>
    public static string MapToPostgresType(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        var config = DaxSharpConfig.Instance;
        var typeMappings = config.PostgreSql.TypeMappings;

        return underlyingType switch
        {
            _ when underlyingType == typeof(bool) => typeMappings.GetValueOrDefault("Boolean", "BOOLEAN"),
            _ when underlyingType == typeof(byte) => typeMappings.GetValueOrDefault("Byte", "SMALLINT"),
            _ when underlyingType == typeof(short) => typeMappings.GetValueOrDefault("Short", "SMALLINT"),
            _ when underlyingType == typeof(int) => typeMappings.GetValueOrDefault("Int", "INTEGER"),
            _ when underlyingType == typeof(long) => typeMappings.GetValueOrDefault("Long", "BIGINT"),
            _ when underlyingType == typeof(float) => typeMappings.GetValueOrDefault("Float", "REAL"),
            _ when underlyingType == typeof(double) => typeMappings.GetValueOrDefault("Double", "DOUBLE PRECISION"),
            _ when underlyingType == typeof(decimal) => typeMappings.GetValueOrDefault("Decimal", "NUMERIC"),
            _ when underlyingType == typeof(DateTime) => typeMappings.GetValueOrDefault("DateTime", "TIMESTAMP"),
            _ when underlyingType == typeof(DateTimeOffset) => typeMappings.GetValueOrDefault("DateTimeOffset", "TIMESTAMPTZ"),
            _ when underlyingType == typeof(TimeSpan) => typeMappings.GetValueOrDefault("TimeSpan", "INTERVAL"),
            _ when underlyingType == typeof(Guid) => typeMappings.GetValueOrDefault("Guid", "UUID"),
            _ when underlyingType == typeof(byte[]) => typeMappings.GetValueOrDefault("ByteArray", "BYTEA"),
            _ when underlyingType == typeof(string) => typeMappings.GetValueOrDefault("String", "TEXT"),
            _ => typeMappings.GetValueOrDefault("Default", "TEXT") // Default to TEXT for unknown types
        };
    }

    /// <summary>
    /// Maps .NET/DAX data types (as string) to PostgreSQL data types.
    /// </summary>
    public static string MapToPostgresType(string dataType)
    {
        if (string.IsNullOrEmpty(dataType))
            return "text";
        
        var config = DaxSharpConfig.Instance;
        var typeMappings = config.PostgreSql.TypeMappings;
        
        // Handle full type names like "System.Int32" or "System.String"
        var typeName = dataType.ToLowerInvariant();
        
        // Remove namespace prefix if present
        if (typeName.Contains('.'))
        {
            typeName = typeName.Substring(typeName.LastIndexOf('.') + 1);
        }
        
        return typeName switch
        {
            "int32" or "int" => typeMappings.GetValueOrDefault("Int32", "integer"),
            "int64" or "long" => typeMappings.GetValueOrDefault("Int64", "bigint"),
            "double" => typeMappings.GetValueOrDefault("Double", "numeric"),
            "float" => typeMappings.GetValueOrDefault("Float", "numeric"),
            "decimal" => typeMappings.GetValueOrDefault("Decimal", "numeric"),
            "datetime" => typeMappings.GetValueOrDefault("DateTime", "timestamp"),
            "string" or "text" => typeMappings.GetValueOrDefault("String", "text"),
            "boolean" or "bool" => typeMappings.GetValueOrDefault("Boolean", "boolean"),
            "byte" => typeMappings.GetValueOrDefault("Byte", "smallint"),
            "short" => typeMappings.GetValueOrDefault("Short", "smallint"),
            "guid" => typeMappings.GetValueOrDefault("Guid", "uuid"),
            "timespan" => typeMappings.GetValueOrDefault("TimeSpan", "interval"),
            _ => typeMappings.GetValueOrDefault("Default", "text") // Default to text for unknown types
        };
    }
}
