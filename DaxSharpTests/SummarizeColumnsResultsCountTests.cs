namespace DaxSharpTests;

using DaxSharp;

public class SummarizeColumnsResultsCountTests
{
/*
EVALUATE
        TOPN(
        1000,
            SUMMARIZECOLUMNS(
                products[productId],
                customers[customerId],
                FILTER(sales, sales[amount] > 1),
                "sum", 1 + MAX(sales[amount]) + SUM(sales[amount])
            )
        )
*/
    [Fact]
    public void SummarizeColumns_NoConstant_MaxCount()
    {
        var sales = Enumerable.Range(0, 5000)
            .Select(i => (productId: i % 1000, customerId: i % 200, amount: i % 100))
            .ToArray();
        var result = sales.SummarizeColumns(
            x => new {x.productId, x.customerId},
            x => x is { amount: > 1 },
            (_, _) => true,
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Max(y => y.amount) + array.Sum(y => y.amount)
                : 0),
            from pId in Enumerable.Range(0, 1000)
            from cId in Enumerable.Range(0, 1000)
            select new { productId = pId, customerId = cId },
            1000
        ).ToList();
        Assert.Equal(1000, result.Count);
    }
}