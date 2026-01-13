using System.Text;
using DaxSharp.Helpers;
using DaxSharp.Models;
using DaxSharp.Postgres;

namespace DaxSharp.OpenAI;

/// <summary>
/// Formats database schema for inclusion in OpenAI prompts.
/// </summary>
internal static class SchemaFormatter
{
    /// <summary>
    /// Formats database schema for inclusion in OpenAI prompt.
    /// </summary>
    public static string FormatSchemaForPrompt(DatabaseSchema schema, string schemaName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"PostgreSQL Schema Name: {schemaName}");
        sb.AppendLine();
        
        // Format tables and columns
        foreach (var table in schema.Tables)
        {
            sb.AppendLine($"Table: {schemaName}.{PostgresIdentifierHelper.EscapeIdentifier(table.TableName)}");
            sb.AppendLine("Columns:");
            foreach (var column in table.Columns)
            {
                var nullableStr = column.IsNullable ? "nullable" : "not null";
                var pgType = PostgresTypeMapper.MapToPostgresType(column.DataType);
                sb.AppendLine($"  - {PostgresIdentifierHelper.EscapeIdentifier(column.Name)} ({pgType}, {nullableStr})");
            }
            sb.AppendLine();
        }
        
        // Format relationships
        if (schema.AllRelationships.Count <= 0)
        {
            return sb.ToString();
        }

        sb.AppendLine("Relationships:");
        foreach (var rel in schema.AllRelationships)
        {
            sb.AppendLine($"  - {schemaName}.{PostgresIdentifierHelper.EscapeIdentifier(rel.FromTable)}.{PostgresIdentifierHelper.EscapeIdentifier(rel.FromColumn)} -> {schemaName}.{PostgresIdentifierHelper.EscapeIdentifier(rel.ToTable)}.{PostgresIdentifierHelper.EscapeIdentifier(rel.ToColumn)} ({rel.RelationshipType})");
        }
        sb.AppendLine();

        return sb.ToString();
    }
}
