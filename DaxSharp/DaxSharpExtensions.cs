using System.Collections;

namespace DaxSharp;

public static class DaxSharpExtensions
{
    public static IEnumerable<(TGrouped grouped, TExpressions expressions)> SummarizeColumns<T, TGrouped,
        TExpressions>(this IEnumerable<T> items, Func<T, TGrouped> groupByBuilder, Func<T?, TGrouped?, bool> filter,
        Func<IEnumerable<T>, TGrouped?, TExpressions?> expressions, int maxCount = int.MaxValue)
        where TGrouped : notnull
    {
        var results = items.Where(item => filter(item, default))
            .GroupBy(groupByBuilder)
            .Where(g => filter(default, g.Key))
            .ToDictionary(x => x.Key, x => expressions(x, x.Key));

        var currentCount = 0;
        foreach (var result in results
                     .Where(result => result.Value is not null))
        {
            if (currentCount++ >= maxCount)
            {
                break;
            }

            yield return (result.Key, result.Value!);
        }
    }

    public static IEnumerable<(TGrouped grouped, TExpressions expressions)> SummarizeColumnsCartesian<T, TGrouped,
        TExpressions>(this IEnumerable<T> items, Func<T, TGrouped> groupByBuilder, Func<T?, TGrouped?, bool> filter,
        Func<IEnumerable<T>, TGrouped?, TExpressions?> expressions, int maxCount = int.MaxValue)
        where TGrouped : notnull
    {
        var arrayItems = items.ToArray();
        var fields = typeof(TGrouped).GetProperties();

        if (fields.Length == 0)
        {
            if (filter(default, default))
            {
                yield return (default!, expressions(arrayItems, default)!);
            }
        }
        else
        {
            var results = arrayItems.Where(item => filter(item, default))
                .GroupBy(groupByBuilder)
                .Where(g => filter(default, g.Key))
                .ToDictionary(x => x.Key, x => expressions(x, x.Key));

            var groupedValues = fields.Select(_ => new HashSet<object>()).ToArray();
            foreach (var item in arrayItems)
            {
                var itemKey = groupByBuilder(item);
                for (var groupIndex = 0; groupIndex < groupedValues.Length; groupIndex++)
                {
                    var value = fields[groupIndex].GetValue(itemKey);
                    groupedValues[groupIndex].Add(value!);
                }
            }

            var groupedLists = groupedValues.Select(x => x.ToList()).ToArray();
            var currentValues = new object?[groupedValues.Length];
            var enumerators = new IEnumerator[groupedLists.Length];

            for (var i = 0; i < enumerators.Length; i++)
            {
                enumerators[i] = groupedLists[i].GetEnumerator();
                if (enumerators[i].MoveNext())
                {
                    currentValues[i] = enumerators[i].Current;
                }
                else
                {
                    throw new InvalidOperationException($"Enumerator at index {i} is empty.");
                }
            }

            var processCartesian = true;
            var currentCount = 0;
            while (processCartesian && currentCount < maxCount)
            {
                var ctor = typeof(TGrouped).GetConstructors()[0];
                var groupKey = (TGrouped)ctor.Invoke(currentValues);

                if (filter(default, groupKey))
                {
                    if (results.TryGetValue(groupKey, out var value))
                    {
                        if (value is not null)
                        {
                            yield return (groupKey, value);
                        }
                    }
                    else
                    {
                        var values = expressions([], groupKey);
                        yield return (groupKey, values!);
                    }

                    currentCount++;
                }

                int groupIndex;
                for (groupIndex = 0; groupIndex < groupedValues.Length; groupIndex++)
                {
                    if (enumerators[groupIndex].MoveNext())
                    {
                        currentValues[groupIndex] = enumerators[groupIndex].Current!;
                        break;
                    }

                    if (groupIndex == groupedValues.Length - 1)
                    {
                        processCartesian = false;
                        break;
                    }

                    enumerators[groupIndex].Reset();
                    enumerators[groupIndex].MoveNext();
                    currentValues[groupIndex] = enumerators[groupIndex].Current!;
                }
            }
        }
    }
}