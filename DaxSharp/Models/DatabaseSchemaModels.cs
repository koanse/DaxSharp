namespace DaxSharp.Models;

/// <summary>
/// Represents a column in a PowerBI table.
/// </summary>
public class TableColumn
{
    /// <summary>
    /// Column name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Column data type (e.g., "Int64", "String", "Double", "DateTime")
    /// </summary>
    public string DataType { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether the column is nullable
    /// </summary>
    public bool IsNullable { get; init; }
}

/// <summary>
/// Represents a relationship between two tables in PowerBI.
/// </summary>
public class TableRelationship
{
    /// <summary>
    /// Source table name
    /// </summary>
    public string FromTable { get; init; } = string.Empty;
    
    /// <summary>
    /// Source column name
    /// </summary>
    public string FromColumn { get; init; } = string.Empty;
    
    /// <summary>
    /// Target table name
    /// </summary>
    public string ToTable { get; init; } = string.Empty;
    
    /// <summary>
    /// Target column name
    /// </summary>
    public string ToColumn { get; init; } = string.Empty;
    
    /// <summary>
    /// Relationship type (e.g., "ManyToOne", "OneToMany", "OneToOne", "ManyToMany")
    /// </summary>
    public string RelationshipType { get; init; } = string.Empty;
}

/// <summary>
/// Represents a table in PowerBI with its columns and relationships.
/// </summary>
public class TableDescription
{
    /// <summary>
    /// Table name
    /// </summary>
    public string TableName { get; init; } = string.Empty;
    
    /// <summary>
    /// List of columns in the table
    /// </summary>
    public List<TableColumn> Columns { get; set; } = [];
    
    /// <summary>
    /// List of relationships where this table is the source (FromTable)
    /// </summary>
    public List<TableRelationship> Relationships { get; set; } = [];
}

/// <summary>
/// Complete database schema description including all tables, columns, and relationships.
/// </summary>
public class DatabaseSchema
{
    /// <summary>
    /// List of all tables in the database
    /// </summary>
    public List<TableDescription> Tables { get; set; } = [];
    
    /// <summary>
    /// List of all relationships in the database
    /// </summary>
    public List<TableRelationship> AllRelationships { get; set; } = [];
}
