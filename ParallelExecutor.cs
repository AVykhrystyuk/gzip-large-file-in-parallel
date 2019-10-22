using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace GZipTest
{
    public static class ParallelExecution
    {
        public static void ForEach<T>(IEnumerable<T> items, Action<T> handler)
        {
            ForEach(items, handler, DegreeOfParallelism.Default);
        }

        public static void ForEach<T>(IEnumerable<T> items, Action<T> handler, DegreeOfParallelism degreeOfParallelism)
        {
            using (var blockingCollection = new BlockingCollection<T>())
            {
                var threads = Enumerable
                    .Range(0, degreeOfParallelism.Value)
                    .Select(index =>
                    {
                        return new Thread(() =>
                        {
                            while (!blockingCollection.IsAddingCompleted || blockingCollection.Count > 0)
                            {
                                if (blockingCollection.TryTake(out var item))
                                {
                                    handler(item);
                                }

                                Thread.Sleep(0);
                            }
                        })
                        {
                            Name = $"{nameof(ParallelExecution)} [{index}]",
                        };
                    })
                    .ToList();

                threads.ForEach(t => t.Start());

                blockingCollection.AddRange(items);
                blockingCollection.CompleteAdding();

                threads.ForEach(t => t.Join());
            }
        }
    }
}
