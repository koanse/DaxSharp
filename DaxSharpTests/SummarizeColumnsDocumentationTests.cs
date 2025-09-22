using System.Text.Json;

namespace DaxSharpTests;

using DaxSharp;

public class SummarizeColumnsDocumentationTests
{
/*
EVALUATE
    SUMMARIZECOLUMNS(
        Sales[Product],
        Sales[Category],
        FILTER(
            Categories,
            Categories[IsActive] = TRUE && Categories[Category] <> "Category1"
        ),
        "Sum", IF(
            ISBLANK(SUM(Sales[Amount])),
            2,
            SUM(Sales[Amount])
        )
    )
*/
    [Fact]
    public void SummarizeColumns_Documentation()
    {
        var data = new[]
        {
            (Product: "Product1", Category: "Category1", IsActive: true, Amount: 10, Quantity: 2),
            (Product: "Product1", Category: "Category2", IsActive: true, Amount: 20, Quantity: 3),
            (Product: "Product2", Category: "Category1", IsActive: true, Amount: 5, Quantity: 1),
            (Product: "Product3", Category: "Category3", IsActive: true, Amount: 15, Quantity: 2)
        };

        var results = data.SummarizeColumns(
            item => new { item.Product, item.Category },
            x => x.IsActive && x.Category != "Category1",
            (_, _) => true,
            (items, _) =>
                items.ToArray() is { Length: > 0 } array
                    ? array.Sum(x => x.Amount)
                    : 2
        ).ToList();

        var groupsString = JsonSerializer.Serialize(results.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(results.Select(x => x.expressions));
        Assert.Equal(4, results.Count);
        Assert.Equal("[{\"Product\":\"Product1\",\"Category\":\"Category1\"}," +
                     "{\"Product\":\"Product1\",\"Category\":\"Category2\"}," +
                     "{\"Product\":\"Product2\",\"Category\":\"Category1\"}," +
                     "{\"Product\":\"Product3\",\"Category\":\"Category3\"}]", groupsString);
        Assert.Equal("[2,20,2,15]", expressionsString);
    }

/*
    EVALUATE
     SUMMARIZECOLUMNS(
        Products[Product],
        Categories[Category],
        FILTER(
            Categories,
            Categories[IsActive] = TRUE && Categories[Category] <> "Category1"
        ),
        "Sum", IF(
            ISBLANK(SUM(Sales[Amount])),
            2,
            SUM(Sales[Amount])
        )
    )
    ORDER BY Products[Product] DESC
*/
    [Fact]
    public void SummarizeColumns_OrderBy_Documentation()
    {
        var data = new[]
        {
            (Product: "Product1", Category: "Category1", IsActive: true, Amount: 10, Quantity: 2),
            (Product: "Product1", Category: "Category2", IsActive: true, Amount: 20, Quantity: 3),
            (Product: "Product2", Category: "Category1", IsActive: true, Amount: 5, Quantity: 1),
            (Product: "Product3", Category: "Category3", IsActive: true, Amount: 15, Quantity: 2)
        };

        var results = data.SummarizeColumns(
            item => new { item.Product, item.Category },
            item => item is { IsActive: true, Category: not "Category1" },
            (_, g) => g is { Category: not "Category1" },
            (items, _) =>
                items.ToArray() is { Length: > 0 } array
                    ? array.Sum(x => x.Amount)
                    : 2,
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(1, 3)
            select new { Product = $"Product{pId}", Category = $"Category{cId}" }
        ).ToList();

        var groupsString = JsonSerializer.Serialize(results.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(results.Select(x => x.expressions));
        Assert.Equal(6, results.Count);
        Assert.Equal("[{\"Product\":\"Product1\",\"Category\":\"Category2\"}," +
                     "{\"Product\":\"Product1\",\"Category\":\"Category3\"}," +
                     "{\"Product\":\"Product2\",\"Category\":\"Category2\"}," +
                     "{\"Product\":\"Product2\",\"Category\":\"Category3\"}," +
                     "{\"Product\":\"Product3\",\"Category\":\"Category2\"}," +
                     "{\"Product\":\"Product3\",\"Category\":\"Category3\"}]", groupsString);
        Assert.Equal("[20,2,2,2,2,15]", expressionsString);
    }

/*
    EVALUATE
     SUMMARIZECOLUMNS(
        Products[Product],
        FILTER(
            Categories,
            Categories[IsActive] = TRUE && Categories[Category] <> "Category1"
        ),
        "Sum", IF(
            ISBLANK(SUM(Sales[Amount])),
            2,
            SUM(Sales[Amount])
        )
    )
    ORDER BY Products[Product] DESC
*/
    [Fact]
    public void SummarizeColumns_Ui_Documentation()
    {
        var data = new[]
        {
            (ProductId: 1, Product: "Product1", Category: "Category1", IsActive: true, Amount: 10, Quantity: 2),
            (ProductId: 2, Product: "Product1", Category: "Category2", IsActive: true, Amount: 20, Quantity: 3),
            (ProductId: 3, Product: "Product2", Category: "Category1", IsActive: true, Amount: 5, Quantity: 1),
            (ProductId: 4, Product: "Product3", Category: "Category3", IsActive: true, Amount: 15, Quantity: 2)
        };

        var results = data.SummarizeColumns(
            item => new { item.ProductId, item.Product },
            item => item is { IsActive: true, Category: not "Category1" },
            (_, _) => true,
            (items, _) => new
            {
                sum = items.ToArray() is { Length: > 0 } array
                    ? array.Sum(x => x.Amount)
                    : 2
            },
            from pId in Enumerable.Range(1, 3)
            select new { ProductId = pId, Product = $"Product{pId}" }
        ).ToList();

        var mermaidPie = results.ToMermaidPieChart("Pie", x => x.Product, x => x.sum.ToString());
        var mermaidLine = results.ToMermaidLineChart("Line", "x", "y",  x => x.ProductId.ToString(), x => x.sum.ToString());
        var mermaidBar = results.ToMermaidBarChart("Bar", "x", "y", x => x.Product, x => x.sum.ToString());
        var markdownTable = results.ToMarkdownTable();
        
        Assert.Equal("pie title Pie\r\n    \"Product1\" : 2\r\n    \"Product2\" : 2\r\n    \"Product3\" : 2\r\n", mermaidPie);
        Assert.Equal("xychart-beta title \"Line\"\r\n    x-axis x  [1,2,3]\r\n    y-axis y line [2,2,2]\r\n", mermaidLine);
        Assert.Equal("xychart-beta title \"Bar\"\r\n    x-axis x  [Product1,Product2,Product3]\r\n    y-axis y bar [2,2,2]\r\n", mermaidBar);
        Assert.Equal("| ProductId | Product | sum |\r\n| --- | --- | --- |\r\n| 1 | Product1 | 2 |\r\n| 2 | Product2 | 2 |\r\n| 3 | Product3 | 2 |\r\n", markdownTable);
    }
}