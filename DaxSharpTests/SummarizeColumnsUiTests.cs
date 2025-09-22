namespace DaxSharpTests;

using DaxSharp;

public class SummarizeColumnsUiTests
{
/*
EVALUATE
    SUMMARIZECOLUMNS(
        products[productId],
        "sum", 1 + MAX(sales[amount]) + SUM(sales[amount])
    )
*/
    [Fact]
    public void SummarizeColumns_MermaidPieChart()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = 10 },
            new { productId = "2", customerId = "2", amount = 15 },
            new { productId = "3", customerId = "3", amount = 5 }
        };

        var result = sales.SummarizeColumns(
            x => new { x.productId },
            _ => true,
            (_, _) => true,
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Max(y => y.amount) + array.Sum(y => y.amount)
                : 0),
            from pId in Enumerable.Range(1, 3)
            select new { productId = $"{pId}" }
        ).ToList();

        var mermaidPie = result.ToMermaidPieChart(
            "Pie",
            x => x.productId,
            x => x.ToString());
        
        Assert.Equal($"pie title Pie{Environment.NewLine}" +
                     $"    \"1\" : 21{Environment.NewLine}" +
                     $"    \"2\" : 31{Environment.NewLine}" +
                     $"    \"3\" : 11{Environment.NewLine}", mermaidPie);
    }

/*
EVALUATE
    SUMMARIZECOLUMNS(
        products[productId],
        "sum", 1 + MAX(sales[amount]) + SUM(sales[amount])
    )
*/
    [Fact]
    public void SummarizeColumns_MermaidLineChart()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = 10 },
            new { productId = "2", customerId = "2", amount = 15 },
            new { productId = "3", customerId = "3", amount = 5 }
        };

        var result = sales.SummarizeColumns(
            x => new { x.productId },
            _ => true,
            (_, _) => true,
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Max(y => y.amount) + array.Sum(y => y.amount)
                : 0),
            from pId in Enumerable.Range(1, 3)
            select new { productId = $"{pId}" }
        ).ToList();

        var mermaidLine = result.ToMermaidLineChart(
            "Line",
            "X",
            "Y",
            x => x.productId,
            x => x.ToString());
        
        Assert.Equal($"xychart-beta title \"Line\"{Environment.NewLine}" +
                     $"    x-axis X  [1,2,3]{Environment.NewLine}" +
                     $"    y-axis Y line [21,31,11]{Environment.NewLine}", mermaidLine);
    }

/*
EVALUATE
    SUMMARIZECOLUMNS(
        products[productId],
        "sum", 1 + MAX(sales[amount]) + SUM(sales[amount])
    )
*/
    [Fact]
    public void SummarizeColumns_MermaidBarChart()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = 10 },
            new { productId = "2", customerId = "2", amount = 15 },
            new { productId = "3", customerId = "3", amount = 5 }
        };

        var result = sales.SummarizeColumns(
            x => new { x.productId },
            _ => true,
            (_, _) => true,
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Max(y => y.amount) + array.Sum(y => y.amount)
                : 0),
            from pId in Enumerable.Range(1, 3)
            select new { productId = $"{pId}" }
        ).ToList();

        var mermaidBar = result.ToMermaidBarChart(
            "Line",
            "X",
            "Y",
            x => x.productId,
            x => x.ToString());
        
        Assert.Equal($"xychart-beta title \"Line\"{Environment.NewLine}" +
                     $"    x-axis X  [1,2,3]{Environment.NewLine}" +
                     $"    y-axis Y bar [21,31,11]{Environment.NewLine}", mermaidBar);
    }

/*
EVALUATE
    SUMMARIZECOLUMNS(
        products[productId],
        "sum", 1 + MAX(sales[amount]) + SUM(sales[amount])
    )
*/
    [Fact]
    public void SummarizeColumns_MarkdownTable()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = 10 },
            new { productId = "2", customerId = "2", amount = 15 },
            new { productId = "3", customerId = "3", amount = 5 }
        };

        var result = sales.SummarizeColumns(
            x => new { x.productId },
            _ => true,
            (_, _) => true,
            (x, g) => new
            {
                sum = 1 + (x.ToArray() is { Length: > 0 } array
                    ? array.Max(y => y.amount) + array.Sum(y => y.amount)
                    : 0)
            },
            from pId in Enumerable.Range(1, 3)
            select new { productId = $"{pId}" }
        ).ToList();

        var markdownTable = result.ToMarkdownTable();
        
        Assert.Equal($"| productId | sum |{Environment.NewLine}" +
                     $"| --- | --- |{Environment.NewLine}" +
                     $"| 1 | 21 |{Environment.NewLine}" +
                     $"| 2 | 31 |{Environment.NewLine}" +
                     $"| 3 | 11 |{Environment.NewLine}", markdownTable);
    }
}