using System.Text.Json;

namespace DaxSharpTests;

using DaxSharp;

public class SummarizeColumnsTests
{
    [Fact]
    public void SummarizeColumns_ConstantInExpressions_Sum_Max()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = 10 },
            new { productId = "2", customerId = "2", amount = 15 },
            new { productId = "3", customerId = "3", amount = 5 }
        }.ToList();

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            (x, g) => x is { amount: > 1 } || g is { productId: not null },
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Max(y => y.amount) + array.Sum(y => y.amount)
                : 0),
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(2, 2)
            select new { productId = $"{pId}", customerId = $"{cId}" }
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));

        Assert.Equal(6, result.Count);
        Assert.Equal("[{\"productId\":\"1\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"1\",\"customerId\":\"3\"}," +
                     "{\"productId\":\"2\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"2\",\"customerId\":\"3\"}," +
                     "{\"productId\":\"3\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"3\",\"customerId\":\"3\"}]", groupsString);
        Assert.Equal("[21,1,31,1,1,11]", expressionsString);
    }

    [Fact]
    public void SummarizeColumns_ConstantInExpressions_Sum()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = (int?)10 },
            new { productId = "2", customerId = "2", amount = (int?)15 },
            new { productId = "3", customerId = "3", amount = (int?)null }
        }.ToList();

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            (x, g) => x is { amount: > 1 or null } || g is { productId: not null },
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Sum(y => y.amount) + array.Sum(y => y.amount)
                : 0),
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(2, 2)
            select new { productId = $"{pId}", customerId = $"{cId}" }
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));
        Assert.Equal(6, result.Count);
        Assert.Equal("[{\"productId\":\"1\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"1\",\"customerId\":\"3\"}," +
                     "{\"productId\":\"2\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"2\",\"customerId\":\"3\"}," +
                     "{\"productId\":\"3\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"3\",\"customerId\":\"3\"}]", groupsString);
        Assert.Equal("[21,1,31,1,1,1]", expressionsString);
    }

    [Fact]
    public void SummarizeColumns_ConstantInExpressions_Sum_Two_Filters()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = (int?)10 },
            new { productId = "2", customerId = "2", amount = (int?)15 },
            new { productId = "3", customerId = "3", amount = (int?)null }
        }.ToList();

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            (x, g) => x is { amount: > 1 and < 100 } || g is { productId: not null },
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Sum(y => y.amount) + array.Sum(y => y.amount)
                : 0),
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(2, 2)
            select new { productId = $"{pId}", customerId = $"{cId}" }
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));
        Assert.Equal(6, result.Count);
        Assert.Equal("[{\"productId\":\"1\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"1\",\"customerId\":\"3\"}," +
                     "{\"productId\":\"2\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"2\",\"customerId\":\"3\"}," +
                     "{\"productId\":\"3\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"3\",\"customerId\":\"3\"}]", groupsString);
        Assert.Equal("[21,1,31,1,1,1]", expressionsString);
    }

    [Fact]
    public void SummarizeColumns_ConstantInExpressions_Sum_Two_Filters_GroupFilter()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = (int?)10 },
            new { productId = "2", customerId = "2", amount = (int?)15 },
            new { productId = "3", customerId = "3", amount = (int?)null }
        }.ToList();

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            (x, g) => x is { amount: > 1 and < 100 } || g is { productId: "2" },
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Sum(y => y.amount) + array.Sum(y => y.amount)
                : 0),
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(2, 2)
            select new { productId = $"{pId}", customerId = $"{cId}" }
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));
        Assert.Equal(2, result.Count);
        Assert.Equal("[{\"productId\":\"2\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"2\",\"customerId\":\"3\"}]", groupsString);
        Assert.Equal("[31,1]", expressionsString);
    }

    [Fact]
    public void SummarizeColumns_ConstantInExpressions_Sum_Two_Filters_GroupFilter_NoData()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = (int?)10 },
            new { productId = "2", customerId = "2", amount = (int?)15 },
            new { productId = "3", customerId = "3", amount = (int?)null }
        }.ToList();

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            (x, g) => x is { amount: > 10 and < 100, productId: "3" } && g is { productId: "3" },
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Sum(y => y.amount) + array.Sum(y => y.amount)
                : 0),
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(2, 2)
            select new { productId = $"{pId}", customerId = $"{cId}" }
        ).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void SummarizeColumns_ConstantInExpressions_Sum_NoFilters()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = (int?)10 },
            new { productId = "2", customerId = "2", amount = (int?)15 },
            new { productId = "3", customerId = "3", amount = (int?)null }
        }.ToList();

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            (_, _) => true,
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Sum(y => y.amount) + array.Sum(y => y.amount)
                : 0),
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(2, 2)
            select new { productId = $"{pId}", customerId = $"{cId}" }
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));
        Assert.Equal(6, result.Count);
        Assert.Equal("[{\"productId\":\"1\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"1\",\"customerId\":\"3\"}," +
                     "{\"productId\":\"2\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"2\",\"customerId\":\"3\"}," +
                     "{\"productId\":\"3\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"3\",\"customerId\":\"3\"}]", groupsString);
        Assert.Equal("[21,1,31,1,1,1]", expressionsString);
    }

    [Fact]
    public void SummarizeColumns_ConstantInExpressions_Sum_NoFilters_NoGrouping()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = (int?)10 },
            new { productId = "2", customerId = "2", amount = (int?)15 },
            new { productId = "3", customerId = "3", amount = (int?)null }
        }.ToList();

        var result = sales.SummarizeColumns(
            _ => new object(),
            (_, _) => true,
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Sum(y => y.amount) + array.Sum(y => y.amount)
                : 0)
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));
        Assert.Single(result);
        Assert.Equal("[null]", groupsString);
        Assert.Equal("[51]", expressionsString);
    }

    [Fact]
    public void SummarizeColumns_NoConstantInExpressions()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = (int?)10 },
            new { productId = "2", customerId = "2", amount = (int?)15 },
            new { productId = "3", customerId = "3", amount = (int?)null }
        }.ToList();

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            (x, g) => x is { customerId: not "2" } || g is { productId: not null, customerId: not "2" },
            (items, g) => items.ToArray() is { Length: > 0 } array
                ? array.Sum(x => x.amount)
                : 2
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));
        Assert.Single(result);
        Assert.Equal("[{\"productId\":\"3\",\"customerId\":\"3\"}]", groupsString);
        Assert.Equal("[0]", expressionsString);
    }

    [Fact]
    public void SummarizeColumns_OrderBy_NoConstantInExpressions()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = 10 },
            new { productId = "2", customerId = "2", amount = 15 },
            new { productId = "3", customerId = "3", amount = 5 }
        }.ToList();

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            (x, g) => x is { amount: > 1 } || g is { productId: not null },
            (x, g) => x.ToArray() is { Length: > 0 } array
                ? array.Max(y => y.amount) + array.Sum(y => y.amount)
                : 0,
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(2, 2)
            select new { productId = $"{pId}", customerId = $"{cId}" }
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));
        Assert.Equal(6, result.Count);
        Assert.Equal("[{\"productId\":\"1\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"1\",\"customerId\":\"3\"}," +
                     "{\"productId\":\"2\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"2\",\"customerId\":\"3\"}," +
                     "{\"productId\":\"3\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"3\",\"customerId\":\"3\"}]", groupsString);
        Assert.Equal("[20,0,30,0,0,10]", expressionsString);
    }

    [Fact]
    public void SummarizeColumns_NoConstantInExpressions_GroupFilter()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = 10 },
            new { productId = "2", customerId = "2", amount = 15 },
            new { productId = "3", customerId = "3", amount = 5 }
        }.ToList();

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            (x, g) => x is { amount: > 1, productId: "3" } || g is { productId: "3", customerId: "3" },
            (x, g) => x.ToArray() is { Length: > 0 } array
                ? array.Max(y => y.amount) + array.Sum(y => y.amount)
                : 0,
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(2, 2)
            select new { productId = $"{pId}", customerId = $"{cId}" }
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));
        Assert.Single(result);
        Assert.Equal("[{\"productId\":\"3\",\"customerId\":\"3\"}]", groupsString);
        Assert.Equal("[10]", expressionsString);
    }
    
    [Fact]
    public void SummarizeColumns_NoConstantInExpressions_MaxCount()
    {
        var sales = Enumerable.Range(0, 5000)
            .Select(i => (productId: i % 1000, customerId: i % 200, amount: i % 100))
            .ToArray();
        var result = sales.SummarizeColumns(
            x => new {x.productId, x.customerId},
            (x, g) => x is { amount: > 1 } || g is { productId: > 0 },
            (x, g) => x.ToArray() is { Length: > 0 } array
                ? array.Max(y => y.amount) + array.Sum(y => y.amount)
                : 0,
            from pId in Enumerable.Range(0, 1000)
            from cId in Enumerable.Range(0, 1000)
            select new { productId = pId, customerId = cId },
            1000
        ).ToList();
        Assert.Equal(1000, result.Count);
    }
}