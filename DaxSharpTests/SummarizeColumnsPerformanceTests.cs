using System.Diagnostics;
using Xunit.Abstractions;

namespace DaxSharpTests;

using DaxSharp;

public class SummarizeColumnsPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public void SummarizeColumnsCartesian_NoConstantInExpressions_100M()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var sales = Enumerable.Range(0, 100000000)
            .Select(i => (productId: i % 1000, customerId: i % 200, amount: i % 100))
            .ToArray();
        stopwatch.Stop();
        output.WriteLine($"Creation: {stopwatch.Elapsed}");
        
        stopwatch.Restart();
        var result = sales.SummarizeColumnsCartesian(
            x => new {x.productId, x.customerId},
            (x, g) => x is { amount: > 1 } || g is { productId: > 0 },
            (x, g) => x.ToArray() is { Length: > 0 } array
                ? array.Max(y => y.amount) + array.Sum(y => y.amount)
                : 0,
            3000
        ).ToList();
        Assert.Equal(3000, result.Count);
        stopwatch.Stop();
        output.WriteLine($"Elapsed: {stopwatch.Elapsed}");
    }
}