using System.Text.Json;

namespace DaxSharpTests;

using DaxSharp;

public class SummarizeColumnsNullAmountTests
{
/*
EVALUATE
    SUMMARIZECOLUMNS(
        products[productId],
        customers[customerId],
        "sum", 1 + SUM(sales[amount])
    )
*/
    [Fact]
    public void SummarizeColumns_Constant_Sum_NoFilter()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = (int?)10 },
            new { productId = "2", customerId = "2", amount = (int?)15 },
            new { productId = "3", customerId = "3", amount = (int?)null }
        };

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            _ => true,
            (_, _) => true,
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Sum(y => y.amount)
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
        Assert.Equal("[11,1,16,1,1,1]", expressionsString);
    }

/*
EVALUATE
    SUMMARIZECOLUMNS(
        products[productId],
        customers[customerId],
        FILTER(sales, sales[amount] > 1 && sales[amount] < 100),
        "sum", 1 + SUM(sales[amount])
    )
*/
    [Fact]
    public void SummarizeColumns_Constant_Sum_TwoFactFilters()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = (int?)10 },
            new { productId = "2", customerId = "2", amount = (int?)15 },
            new { productId = "3", customerId = "3", amount = (int?)null }
        };

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            x => x is { amount: > 1 and < 100 },
            (items, _) => items.Any(),
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Sum(y => y.amount)
                : 0),
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(2, 2)
            select new { productId = $"{pId}", customerId = $"{cId}" }
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));
        Assert.Equal(2, result.Count);
        Assert.Equal("[{\"productId\":\"1\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"2\",\"customerId\":\"2\"}]", groupsString);
        Assert.Equal("[11,16]", expressionsString);
    }

/*
EVALUATE
    SUMMARIZECOLUMNS(
        products[productId],
        customers[customerId],
        FILTER(sales, sales[amount] > 1 && sales[amount] < 100),
        FILTER(products, products[productId] = 2),
        "sum", 1 + SUM(sales[amount])
    )
*/
    [Fact]
    public void SummarizeColumns_Constant_Sum_TwoFactFilters_GroupFilter()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = (int?)10 },
            new { productId = "2", customerId = "2", amount = (int?)15 },
            new { productId = "3", customerId = "3", amount = (int?)null }
        };

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            x => x is { amount: > 1 and < 100, productId: "2" },
            (x, g) => g is { productId: "2" } && x.Any(),
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Sum(y => y.amount)
                : 0),
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(2, 2)
            select new { productId = $"{pId}", customerId = $"{cId}" }
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));
        Assert.Single(result);
        Assert.Equal("[{\"productId\":\"2\",\"customerId\":\"2\"}]", groupsString);
        Assert.Equal("[16]", expressionsString);
    }

/*
EVALUATE
    SUMMARIZECOLUMNS(
        products[productId],
        customers[customerId],
        FILTER(sales, sales[amount] > 1 && sales[amount] < 100),
        FILTER(products, products[productId] = 3),
        "sum", 1 + SUM(sales[amount])
    )
*/
    [Fact]
    public void SummarizeColumns_Constant_Sum_TwoFactFilters_GroupFilter_NoData()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = (int?)10 },
            new { productId = "2", customerId = "2", amount = (int?)15 },
            new { productId = "3", customerId = "3", amount = (int?)null }
        };

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            x => x is { amount: > 10 and < 100, productId: "3" },
            (x, g) => g is { productId: "3" } && x.Any(),
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Sum(y => y.amount)
                : 0),
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(2, 2)
            select new { productId = $"{pId}", customerId = $"{cId}" }
        ).ToList();

        Assert.Empty(result);
    }

/*
EVALUATE
    SUMMARIZECOLUMNS(
        "sum", 1 + SUM(sales[amount])
    )
*/
    [Fact]
    public void SummarizeColumns_Constant_Sum_NoFilters_NoGrouping()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = (int?)10 },
            new { productId = "2", customerId = "2", amount = (int?)15 },
            new { productId = "3", customerId = "3", amount = (int?)null }
        };

        var result = sales.SummarizeColumns(
            _ => new object(),
            _ => true,
            (_, _) => true,
            (x, g) => 1 + (x.ToArray() is { Length: > 0 } array
                ? array.Sum(y => y.amount)
                : 0)
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));
        Assert.Single(result);
        Assert.Equal("[null]", groupsString);
        Assert.Equal("[26]", expressionsString);
    }

/*
EVALUATE
    SUMMARIZECOLUMNS(
        products[productId],
        customers[customerId],
        FILTER(sales, sales[customerId] <> 2),
        "sum", 1 + SUM(sales[amount])
    )
*/
    [Fact]
    public void SummarizeColumns_NoConstant_GroupFactsFilter()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = (int?)10 },
            new { productId = "2", customerId = "2", amount = (int?)15 },
            new { productId = "3", customerId = "3", amount = (int?)null }
        };

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            x => x is { customerId: not "2" },
            (x, g) => g is { customerId: not "2" } && x.Any(),
            (items, g) =>
                1 + (items.ToArray() is { Length: > 0 } array
                    ? array.Sum(x => x.amount)
                    : 0),
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(2, 2)
            select new { productId = $"{pId}", customerId = $"{cId}" }
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));
        Assert.Single(result);
        Assert.Equal("[{\"productId\":\"3\",\"customerId\":\"3\"}]", groupsString);
        Assert.Equal("[1]", expressionsString);
    }

/*
EVALUATE
    SUMMARIZECOLUMNS(
        products[productId],
        customers[customerId],
        FILTER(customers, customers[customerId] <> 2),
        "sum", 1 + SUM(sales[amount])
    )
*/
    [Fact]
    public void SummarizeColumns_NoConstant_GroupDictionaryFilter()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = (int?)10 },
            new { productId = "2", customerId = "2", amount = (int?)15 },
            new { productId = "3", customerId = "3", amount = (int?)null }
        };

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            _ => true,
            (x, g) => g is { customerId: not "2" },
            (items, g) =>
                1 + (items.ToArray() is { Length: > 0 } array
                    ? array.Sum(x => x.amount)
                    : 0),
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(2, 2)
            select new { productId = $"{pId}", customerId = $"{cId}" }
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));
        Assert.Equal(3, result.Count);
        Assert.Equal("[{\"productId\":\"1\",\"customerId\":\"3\"}," +
                     "{\"productId\":\"2\",\"customerId\":\"3\"}," +
                     "{\"productId\":\"3\",\"customerId\":\"3\"}]", groupsString);
        Assert.Equal("[1,1,1]", expressionsString);
    }
}