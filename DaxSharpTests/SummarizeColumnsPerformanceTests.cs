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
				2,
				SUM(Sales[Amount])
			)
		)
	)
*/
    [Fact]
    public void SummarizeColumnsCartesian_NoConstantInExpressions_100M()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var sales = Enumerable.Range(0, 100000000)
            .Select(i => (productId: i % 1000000, customerId: i % 1000000, amount: i % 100))
            .ToArray();
        stopwatch.Stop();
        output.WriteLine($"Creation: {stopwatch.Elapsed}");
        
        stopwatch.Restart();
        var result = sales.SummarizeColumnsCartesian(
            x => new {x.productId, x.customerId},
            (_, _) => true,
            (x, g) => x.ToArray() is { Length: > 0 } array
                ? array.Sum(y => y.amount)
                : 1,
            1000
        ).ToList();
        Assert.Equal(3000, result.Count);
        stopwatch.Stop();
        output.WriteLine($"Elapsed: {stopwatch.Elapsed}");
    }
}