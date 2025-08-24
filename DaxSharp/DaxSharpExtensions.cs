using System.Collections.Concurrent;

namespace DaxSharp;

public static class DaxSharpExtensions
{
    public static IEnumerable<(TGrouped? grouped, TExpressions expressions)> SummarizeColumns<T, TGrouped, TExpressions>(
        this T[] items,
        Func<T, TGrouped> groupBy,
        Func<T?, bool> itemFilter,
        Func<IEnumerable<T?>, TGrouped?, bool> groupFilter,
        Func<IEnumerable<T>, TGrouped?, TExpressions?> expressions,
        IEnumerable<TGrouped>? orderBy = null,
        int maxCount = int.MaxValue)
        where TGrouped : notnull
    {
        if (typeof(TGrouped).GetProperties().Length == 0)
        {
            if (groupFilter(items, default) && expressions(items, default) is { } result)
            {
                yield return (default, result);
            }
        }
        else
        {
            double chunkCount = Environment.ProcessorCount;
            var chunkSize = (int)Math.Ceiling(items.Length / chunkCount);
            var processedGroupCount = 0;
            List<TGrouped> groupKeys = [];
            var groupedItems = new ConcurrentDictionary<TGrouped, List<T>>();

            void ScanItems()
            {
                var tasks = new List<Task>();
                for (var i = 0; i < chunkCount; i++)
                {
                    var startIndex = i * chunkSize;
                    var endIndex = Math.Min(startIndex + chunkSize, items.Length);
                    tasks.Add(Task.Run(() =>
                    {
                        for (var index = startIndex; index < endIndex; index++)
                        {
                            var item = items[index];
                            var itemGroupKey = groupBy(items[index]);
                            if (!itemFilter(item) || !groupedItems.TryGetValue(itemGroupKey, out var groupItems))
                            {
                                continue;
                            }

                            lock (groupItems)
                            {
                                groupItems.Add(item);
                            }
                        }
                    }));
                }

                Task.WaitAll(tasks);
            }
            
            IEnumerable<(TGrouped group, TExpressions result)> CalculatedGroups(List<TGrouped> keys,
                ConcurrentDictionary<TGrouped, List<T>> grouped)
            {
                foreach (var group in keys)
                {
                    if (!grouped.TryGetValue(group, out var groupItems))
                    {
                        groupItems = [];
                    }

                    if (groupFilter(groupItems, group) && expressions(groupItems, group) is { } result)
                    {
                        processedGroupCount++;
                        yield return (group, result);
                    }

                    if (processedGroupCount >= maxCount)
                    {
                        yield break;
                    }
                }
            }

            var scanChunkSize = maxCount;
            foreach (var groupKey in orderBy ?? items.Select(groupBy))
            {
                groupedItems.TryAdd(groupKey, []);
                groupKeys.Add(groupKey);

                if (groupKeys.Count < scanChunkSize)
                {
                    continue;
                }

                ScanItems();
                foreach (var group in CalculatedGroups(groupKeys, groupedItems))
                {
                    yield return group;
                }

                if (processedGroupCount >= maxCount)
                {
                    yield break;
                }

                groupKeys = [];
                groupedItems = [];
                scanChunkSize = items.Length;
            }

            if (groupKeys.Count == 0)
            {
                yield break;
            }

            ScanItems();
            foreach (var group in CalculatedGroups(groupKeys, groupedItems))
            {
                yield return group;
            }
        }
    }
}