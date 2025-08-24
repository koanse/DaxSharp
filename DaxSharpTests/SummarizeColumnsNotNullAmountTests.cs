using System.Text.Json;

namespace DaxSharpTests;

using DaxSharp;

public class SummarizeColumnsNotNullAmountTests
{
/*
EVALUATE
    SUMMARIZECOLUMNS(
        products[productId],
        customers[customerId],
        "sum", 1 + MAX(sales[amount]) + SUM(sales[amount])
    )
*/
    [Fact]
    public void SummarizeColumns_Constant_Max_Sum_NoFilter()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = 10 },
            new { productId = "2", customerId = "2", amount = 15 },
            new { productId = "3", customerId = "3", amount = 5 }
        };

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            _ => true,
            (_, _) => true,
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

/*
EVALUATE
    SUMMARIZECOLUMNS(
        products[productId],
        customers[customerId],
        "sum", MAX(sales[amount]) + SUM(sales[amount])
    )
*/
    [Fact]
    public void SummarizeColumns_NoConstant_Max_Sum_NoFilter()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = 10 },
            new { productId = "2", customerId = "2", amount = 15 },
            new { productId = "3", customerId = "3", amount = 5 }
        };

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            _ => true,
            (x, _) => x.Any(),
            (x, g) =>
                x.ToArray() is { Length: > 0 } array
                    ? array.Max(y => y.amount) + array.Sum(y => y.amount)
                    : 0,
            from pId in Enumerable.Range(1, 3)
            from cId in Enumerable.Range(2, 2)
            select new { productId = $"{pId}", customerId = $"{cId}" }
        ).ToList();

        var groupsString = JsonSerializer.Serialize(result.Select(x => x.grouped));
        var expressionsString = JsonSerializer.Serialize(result.Select(x => x.expressions));
        Assert.Equal(3, result.Count);
        Assert.Equal("[{\"productId\":\"1\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"2\",\"customerId\":\"2\"}," +
                     "{\"productId\":\"3\",\"customerId\":\"3\"}]", groupsString);
        Assert.Equal("[20,30,10]", expressionsString);
    }

/*
EVALUATE
    SUMMARIZECOLUMNS(
        products[productId],
        customers[customerId],
        FILTER(sales, sales[amount] > 1 && sales[productId] = 3 && sales[customerId] = 3),
        "sum", MAX(sales[amount]) + SUM(sales[amount])
    )
*/
    [Fact]
    public void SummarizeColumns_NoConstant_ItemAndGroupFilter()
    {
        var sales = new[]
        {
            new { productId = "1", customerId = "2", amount = 10 },
            new { productId = "2", customerId = "2", amount = 15 },
            new { productId = "3", customerId = "3", amount = 5 }
        };

        var result = sales.SummarizeColumns(
            x => new { x.productId, x.customerId },
            x => x is { amount: > 1, productId: "3", customerId: "3" },
            (x, g) => g is { productId: "3", customerId: "3" },
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
}