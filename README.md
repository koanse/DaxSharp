# DaxSharp

[**DaxSharp**](https://github.com/koanse/DaxSharp) is a .NET utility library that brings DAX-style summarization capabilities to LINQ collections. It offers flexible grouping, filtering, and aggregation of in-memory data structures in a concise, expressive way.

## 📦 Installation

Install via NuGet:

```bash
dotnet add package DaxSharp
```

## 🚀 Features
- Perform DAX-like SUMMARIZECOLUMNS on in-memory collections.
- Filter data before aggregation.
- Compute multiple aggregation expressions.
- Handle sparse or missing group combinations with Cartesian expansion.
- Optional ordering of results through the `orderBy` parameter.

## 🧪 Usage
### SummarizeColumns
Groups and filters items, then computes specified aggregations.

```csharp
using DaxSharp;
var data = new[]
{
    (Product: "Product1", Category: "Category1", IsActive: true, Amount: 10, Quantity: 2),
    (Product: "Product1", Category: "Category2", IsActive: true, Amount: 20, Quantity: 3),
    (Product: "Product2", Category: "Category1", IsActive: true, Amount: 5, Quantity: 1),
    (Product: "Product3", Category: "Category3", IsActive: true, Amount: 15, Quantity: 2)
}.ToList();

var results = data.SummarizeColumns(
    item => new { item.Product, item.Category },
    (_, _) => true,
    (items, g) =>
        items.Where(x => x.Category != "Category1" && x.IsActive).ToArray() is { Length: > 0 } array
            ? array.Sum(x => x.Amount)
            : 2
).ToList();

```

The results are:
- Product1, Category2, 2 
- Product1, Category2, 20
- Product2, Category1, 2
- Product3, Category3, 15

DAX:
```
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
```

When the `orderBy` parameter is provided, the method processes groups in the specified order and includes cartesian combinations - meaning it will generate results for all combinations specified in the `orderBy` collection when aggregations on missing data aren't all null or zero.

```csharp
using DaxSharp;
var data = new[]
{
    (Product: "Product1", Category: "Category1", IsActive: true, Amount: 10, Quantity: 2),
    (Product: "Product1", Category: "Category2", IsActive: true, Amount: 20, Quantity: 3),
    (Product: "Product2", Category: "Category1", IsActive: true, Amount: 5, Quantity: 1),
    (Product: "Product3", Category: "Category3", IsActive: true, Amount: 15, Quantity: 2)
}.ToList();

var results = data.SummarizeColumns(
    item => new { item.Product, item.Category },
    (item, group) => item is { IsActive: true, Category: not "Category1" } || group is { Category: not "Category1" },
    (items, group) =>
        items.ToArray() is { Length: > 0 } array
            ? array.Sum(x => x.Amount)
            : 2,
    from pId in Enumerable.Range(1, 3).OrderByDescending(x => x)
    from cId in Enumerable.Range(1, 3)
    select new { Product = $"Product{pId}", Category = $"Category{cId}" }
);
```

The results are:
- Product3, Category2, 2
- Product3, Category3, 15
- Product2, Category2, 2
- Product2, Category3, 2
- Product1, Category2, 20
- Product1, Category3, 2

DAX:
```
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
```

## 🛠️ API Reference
`SummarizeColumns<T, TGrouped, TExpressions>`
```csharp
public static IEnumerable<(TGrouped grouped, TExpressions expressions)> SummarizeColumns<T, TGrouped, TExpressions>(
    this IEnumerable<T> items,
    Func<T, TGrouped> groupBy,
    Func<T?, TGrouped?, bool> filter,
    Func<IEnumerable<T>, TGrouped?, TExpressions?> expressions,
    IEnumerable<TGrouped>? orderBy = null,
    int maxCount = int.MaxValue)
    where TGrouped : notnull
```

## ⚡ Performance
DaxSharp is optimized for high-performance data processing with parallel execution.
The library leverages multi-threading and efficient memory management to handle large datasets efficiently.

### Performance Test Examples

**100 Million Rows Test**
Handles 100 million fact table rows in ~0.7 seconds
```csharp
using DaxSharp;
using System.Diagnostics;

var stopwatch = new Stopwatch();
stopwatch.Start();

// Create 100 million fact table rows
var sales = Enumerable.Range(0, 100000000)
    .Select(i => (productId: i % 1000000, customerId: i % 1000000, amount: i % 100))
    .ToArray();

stopwatch.Stop();
Console.WriteLine($"Data creation: {stopwatch.Elapsed}");

stopwatch.Restart();

// Process with SummarizeColumns - equivalent to DAX TOPN(1000, SUMMARIZECOLUMNS(...))
var result = sales.SummarizeColumns(
    x => new { x.productId, x.customerId },
    (_, _) => true,
    (x, g) => x.ToArray() is { Length: > 0 } array
        ? array.Sum(y => y.amount)
        : 1,
    from pId in Enumerable.Range(0, 1000000)
    from cId in Enumerable.Range(0, 1000000)
    select new { productId = pId, customerId = cId },
    1000
).ToList();

stopwatch.Stop();
Console.WriteLine($"Processing: {stopwatch.Elapsed}");
```

**1 Billion Rows Test**
Handles 1 billion fact table rows in ~4.4 seconds.
```csharp
using DaxSharp;
using System.Diagnostics;

var stopwatch = new Stopwatch();
stopwatch.Start();

// Create 1 billion fact table rows
var sales = Enumerable.Range(0, 1000000000)
    .Select(i => (productId: i % 1000000, customerId: i % 1000000, amount: i % 100))
    .ToArray();

stopwatch.Stop();
Console.WriteLine($"Data creation: {stopwatch.Elapsed}");

stopwatch.Restart();

// Process with SummarizeColumns
var result = sales.SummarizeColumns(
    x => new { x.productId, x.customerId },
    (_, _) => true,
    (x, g) => x.ToArray() is { Length: > 0 } array
        ? array.Sum(y => y.amount)
        : 1,
    from pId in Enumerable.Range(0, 1000000)
    from cId in Enumerable.Range(0, 1000000)
    select new { productId = pId, customerId = cId },
    1000
).ToList();

stopwatch.Stop();
Console.WriteLine($"Processing: {stopwatch.Elapsed}");
```

DAX:
```
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
```

## ⚙️ Internals

Cartesian expansion in `SummarizeColumns` fills in missing group key combinations with expression results.

Skips results with all null expressions unless expansion is required.

When `orderBy` is specified, the method ensures all combinations in the `orderBy` collection are included in the results.

## 📄 License
MIT