namespace DaxSharpTests;

using DaxSharp;

public class SummarizeColumnsDocumentationTests
{
    [Fact]
    public void SummarizeColumns_Documentation()
    {
        var data = new[]
        {
            (Product: "Product1", Category: "Category1", IsActive: true, Amount: 10, Quantity: 2),
            (Product: "Product1", Category: "Category2", IsActive: true, Amount: 20, Quantity: 3),
            (Product: "Product2", Category: "Category1", IsActive: true, Amount: 5, Quantity: 1),
            (Product: "Product3", Category: "Category3", IsActive: true, Amount: 15, Quantity: 2)
        }.ToList();

        var results = data.SummarizeColumns(
            item => new { item.Product, item.Category },
            (item, g) => item is { IsActive: true, Category: not "Category1" } || g is { Category: not "Category1" },
            (items, g) =>
                items.ToArray() is { Length: > 0 } array
                    ? array.Sum(x => x.Amount)
                    : 2
        ).ToList();
        
        Assert.Equal(2, results.Count);
    }
    
    [Fact]
    public void SummarizeColumnsCartesian_Documentation()
    {
        var data = new[]
        {
            (Product: "Product1", Category: "Category1", IsActive: true, Amount: 10, Quantity: 2),
            (Product: "Product1", Category: "Category2", IsActive: true, Amount: 20, Quantity: 3),
            (Product: "Product2", Category: "Category1", IsActive: true, Amount: 5, Quantity: 1),
            (Product: "Product3", Category: "Category3", IsActive: true, Amount: 15, Quantity: 2)
        }.ToList();

        var results = data.SummarizeColumnsCartesian(
            item => new { item.Product, item.Category },
            (item, g) => item is { IsActive: true, Category: not "Category1" } || g is { Category: not "Category1" },
            (items, g) =>
                items.ToArray() is { Length: > 0 } array
                    ? array.Sum(x => x.Amount)
                    : 2
        ).ToList();
        
        Assert.Equal(6, results.Count);
    }
}