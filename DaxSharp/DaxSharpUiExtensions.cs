using System.Text;
using System.Reflection;

namespace DaxSharp;

public static class DaxSharpUiExtensions
{
    public static string ToMermaidPieChart<TGrouped, TExpressions>(
        this IEnumerable<(TGrouped? grouped, TExpressions expressions)> results,
        string title,
        Func<TGrouped, string> getName,
        Func<TExpressions, string> getValue
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine($"pie title {title}");

        foreach (var (group, expressions) in results)
        {
            var groupNameString = group != null ? getName(group) : string.Empty;
            sb.AppendLine($"    \"{groupNameString}\" : {getValue(expressions)}");
        }

        return sb.ToString();
    }

    public static string ToMermaidLineChart<TGrouped, TExpressions>(
        this IEnumerable<(TGrouped? grouped, TExpressions expressions)> results,
        string title,
        string xTitle,
        string yTitle,
        Func<TGrouped, string> getXValue,
        Func<TExpressions, string> getYValue
    ) => ToMermaidChart(results, title, xTitle, yTitle, string.Empty, "line", getXValue, getYValue);

    public static string ToMermaidBarChart<TGrouped, TExpressions>(
        this IEnumerable<(TGrouped? grouped, TExpressions expressions)> results,
        string title,
        string xTitle,
        string yTitle,
        Func<TGrouped, string> getXValue,
        Func<TExpressions, string> getYValue
    ) => ToMermaidChart(results, title, xTitle, yTitle, string.Empty, "bar", getXValue, getYValue);
    
    public static string ToMarkdownTable<TGrouped, TExpressions>(
        this IEnumerable<(TGrouped? grouped, TExpressions expressions)> results)
    {
        var resultsList = results.ToList();

        var sb = new StringBuilder();
        var groupProperties = typeof(TGrouped).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();
        
        var expressionProperties = typeof(TExpressions).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();
        
        var headers = groupProperties.Select(prop => prop.Name).ToList();
        headers.AddRange(expressionProperties.Select(prop => prop.Name));

        sb.AppendLine($"| {string.Join(" | ", headers)} |");
        sb.AppendLine($"| {string.Join(" | ", headers.Select(_ => "---"))} |");
        
        foreach (var (group, expressions) in resultsList)
        {
            var rowValues = groupProperties
                .Select(prop => group != null ? prop.GetValue(group) : null)
                .Select(FormatMarkdownValue).ToList();
            rowValues.AddRange(expressionProperties
                .Select(prop => expressions != null ? prop.GetValue(expressions) : null)
                .Select(FormatMarkdownValue));

            if (rowValues.Count == 0)
            {
                rowValues.Add(group?.ToString() ?? string.Empty);
                rowValues.Add(expressions?.ToString() ?? string.Empty);
            }
            
            sb.AppendLine($"| {string.Join(" | ", rowValues)} |");
        }
        
        return sb.ToString();
    }

    private static string FormatMarkdownValue(object? value)
        => value switch
        {
            string str => str,
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            decimal dec => dec.ToString("F2"),
            double dbl => dbl.ToString("F2"),
            float flt => flt.ToString("F2"),
            _ => value?.ToString() ?? string.Empty
        };

    private static string ToMermaidChart<TGrouped, TExpressions>(
        IEnumerable<(TGrouped? grouped, TExpressions expressions)> results,
        string title, string xTitle, string yTitle, string xChartType, string yChartType,
        Func<TGrouped, string> getXValue, Func<TExpressions, string> getYValue)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"xychart-beta title \"{title}\"");

        var xItems = new List<string>();
        var yItems = new List<string>();

        foreach (var (group, expressions) in results)
        {
            xItems.Add(group != null ? $"{getXValue(group)}" : string.Empty);
            yItems.Add(expressions != null ? $"{getYValue(expressions)}" : string.Empty);
        }
        
        sb.AppendLine($"    x-axis {xTitle} {xChartType} [{string.Join(',', xItems)}]");
        sb.AppendLine($"    y-axis {yTitle} {yChartType} [{string.Join(',', yItems)}]");

        return sb.ToString();
    }
}