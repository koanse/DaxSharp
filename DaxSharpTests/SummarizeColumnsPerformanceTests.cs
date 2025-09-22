using System.Diagnostics;
using Xunit.Abstractions;

namespace DaxSharpTests;

using DaxSharp;

public class SummarizeColumnsPerformanceTests(ITestOutputHelper output)
{
/*
EVALUATE
    TOPN(
        1000,
        SUMMARIZECOLUMNS(
            Products[ProductId],
            Categories[CategoryId],
            "Sum", IF(
                ISBLANK(SUM(Sales[Amount])),
                1,
                SUM(Sales[Amount])
            )
        )
    )
*/
    [Fact]
    public void SummarizeColumns_NoConstant_100M()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var sales = Enumerable.Range(0, 100000000)
            .Select(i => (productId: i % 1000000, customerId: i % 1000000, amount: i % 100))
            .ToArray();
        stopwatch.Stop();
        output.WriteLine($"Creation: {stopwatch.Elapsed}");
        
        stopwatch.Restart();
        var result = sales.SummarizeColumns(
            x => new {x.productId, x.customerId},
            _ => true,
            (_, _) => true,
            (x, g) => x.ToArray() is { Length: > 0 } array
                ? array.Sum(y => y.amount)
                : 1,
            from pId in Enumerable.Range(0, 1000000)
            from cId in Enumerable.Range(0, 1000000)
            select new { productId = pId, customerId = cId },
            1000
        ).ToList();
        Assert.Equal(1000, result.Count);
        stopwatch.Stop();
        output.WriteLine($"Elapsed: {stopwatch.Elapsed}");
    }
    
/*
EVALUATE
    TOPN(
        1000,
        SUMMARIZECOLUMNS(
            Products[ProductId],
            Categories[CategoryId],
            "Sum", IF(
                ISBLANK(SUM(Sales[Amount])),
                1,
                SUM(Sales[Amount])
            )
        )
    )
*/
    [Fact]
    public void SummarizeColumns_Constant_1000M()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var sales = Enumerable.Range(0, 1000000000)
            .Select(i => (productId: i % 1000000, customerId: i % 1000000, amount: i % 100))
            .ToArray();
        stopwatch.Stop();
        output.WriteLine($"Creation: {stopwatch.Elapsed}");
        
        stopwatch.Restart();
        var result = sales.SummarizeColumns(
            x => new {x.productId, x.customerId},
            _ => true,
            (_, _) => true,
            (x, g) => x.ToArray() is { Length: > 0 } array
                ? array.Sum(y => y.amount)
                : 1,
            from pId in Enumerable.Range(0, 1000000)
            from cId in Enumerable.Range(0, 1000000)
            select new { productId = pId, customerId = cId },
            1000
        ).ToList();
        Assert.Equal(1000, result.Count);
        stopwatch.Stop();
        output.WriteLine($"Elapsed: {stopwatch.Elapsed}");
    }

/*
EVALUATE
    TOPN(
        1000,
        SUMMARIZECOLUMNS(
            Products[ProductId],
            Categories[CategoryId],
            "Sum", BLANK()
            )
        )
    )
*/
    [Fact]
    public void SummarizeColumns_AllNullsInExpressions_10K()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var sales = Enumerable.Range(0, 10000)
            .Select(i => (productId: i % 100, customerId: i % 200, amount: i % 200))
            .ToArray();
        stopwatch.Stop();
        output.WriteLine($"Creation: {stopwatch.Elapsed}");
        
        stopwatch.Restart();
        var result = sales.SummarizeColumns(
            x => new {x.productId, x.customerId},
            _ => true,
            (_, _) => true,
            (x, g) => (int?)null,
            from pId in Enumerable.Range(0, 100)
            from cId in Enumerable.Range(0, 100)
            select new { productId = pId, customerId = cId },
            1000
        ).ToList();
        Assert.Empty(result);
        stopwatch.Stop();
        output.WriteLine($"Elapsed: {stopwatch.Elapsed}");
    }
}