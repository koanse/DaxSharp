using System.Collections.Concurrent;

namespace DaxSharp;

public static class DaxSharpExtensions
{
    public static IEnumerable<(TGrouped grouped, TExpressions expressions)> SummarizeColumns<T, TGrouped, TExpressions>(
        this IEnumerable<T> items,
        Func<T, TGrouped> groupBy,
        Func<T?, TGrouped?, bool> filter,
        Func<IEnumerable<T>, TGrouped?, TExpressions?> expressions,
        IEnumerable<TGrouped>? orderBy = null,
        int maxCount = int.MaxValue)
        where TGrouped : notnull
    {
        var arrayItems = items.ToArray();

        if (typeof(TGrouped).GetProperties().Length == 0)
        {
            if (filter(default, default))
            {
                yield return (default!, expressions(arrayItems, default)!);
            }
        }
        else
        {
            var resultItems = new ConcurrentDictionary<TGrouped, List<T>>();
            var resultGroups = new HashSet<TGrouped>();

            var currentCount = 0;
            foreach (var groupKey in orderBy ?? arrayItems.Select(groupBy))
            {
                if (currentCount >= maxCount)
                {
                    break;
                }

                if (!filter(default, groupKey))
                {
                    continue;
                }
                
                resultItems.TryAdd(groupKey, []);
                resultGroups.Add(groupKey);
                currentCount++;
            }

            const double chunkNumber = 4.0;
            var chunkSize = (int)Math.Ceiling(arrayItems.Length / chunkNumber);
            var tasks = new List<Task>();

            for (var i = 0; i < chunkNumber; i++)
            {
                var startIndex = i * chunkSize;
                var endIndex = Math.Min(startIndex + chunkSize, arrayItems.Length);
                
                if (startIndex < arrayItems.Length)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        arrayItems.Skip(startIndex).Take(endIndex - startIndex)
                            .Where(item => filter(item, default))
                            .GroupBy(groupBy)
                            .Where(g => filter(default, g.Key))
                            .ToList()
                            .ForEach(x =>
                            {
                                if (!resultItems.TryGetValue(x.Key, out var value))
                                {
                                    return;
                                }

                                lock (value)
                                {
                                    value.AddRange(x);
                                }
                            });
                    }));
                }
            }

            Task.WaitAll(tasks.ToArray());

            foreach (var groupKey in resultGroups.Where(groupKey => filter(default, groupKey)))
            {
                if (resultItems.TryGetValue(groupKey, out var value))
                {
                    yield return (groupKey, expressions(value, groupKey)!);
                }
                else
                {
                    yield return (groupKey, expressions([], groupKey)!);
                }
            }
        }
    }
}