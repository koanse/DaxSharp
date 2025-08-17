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
            (_, _) => true,
            (items, g) =>
                items.Where(x => x.IsActive && x.Category != "Category1").ToArray() is { Length: > 0 } array
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
            (item, g) => item is { IsActive: true, Category: not "Category1" } || g is { Category: not "Category1" },
            (items, g) =>
                items.ToArray() is { Length: > 0 } array
                    ? array.Sum(x => x.Amount)
                    : 2,
            from pId in Enumerable.Range(1, 3).OrderByDescending(x => x)
            from cId in Enumerable.Range(1, 3)
            select new { Product = $"Product{pId}", Category = $"Category{cId}" }
        ).ToList();

        var groupsString = JsonSerializer.Serialize(results.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(results.Select(x => x.expressions));
        Assert.Equal(6, results.Count);
        Assert.Equal("[{\"Product\":\"Product3\",\"Category\":\"Category2\"}," +
                     "{\"Product\":\"Product3\",\"Category\":\"Category3\"}," +
                     "{\"Product\":\"Product2\",\"Category\":\"Category2\"}," +
                     "{\"Product\":\"Product2\",\"Category\":\"Category3\"}," +
                     "{\"Product\":\"Product1\",\"Category\":\"Category2\"}," +
                     "{\"Product\":\"Product1\",\"Category\":\"Category3\"}]", groupsString);
        Assert.Equal("[2,15,2,2,20,2]", expressionsString);
    }
}