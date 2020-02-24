using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace GZipTest
{
    public static class ParallelExecution
    {
        public static IReadOnlyCollection<Exception> ForEach<T>(
            IEnumerable<T> items,
            Action<T> handleItem,
            DegreeOfParallelism? degreeOfParallelism = default,
            CancellationToken cancellationToken = default)
        {
            var threadCount = (degreeOfParallelism ?? DegreeOfParallelism.Default).Value;
            var exceptions = new ConcurrentBag<Exception>();

            using var blockingItems = new BlockingCollection<T>(new ConcurrentQueue<T>());

            var threads = Enumerable
                .Range(0, threadCount)
                .Select(index => new Thread(() =>
                {
                    ForEachThreadLoop(
                        blockingItems,
                        handleItem,
                        exceptions,
                        cancellationToken);
                })
                {
                    Name = $"{nameof(ParallelExecution)} [{index}]",
                })
                .ToList();

            threads.ForEach(t => t.Start());

            blockingItems.AddRangeSafe(items, cancellationToken);
            blockingItems.CompleteAdding();

            threads.ForEach(t => t.Join());

            if (cancellationToken.IsCancellationRequested)
            {
                exceptions.Add(new OperationCanceledException(cancellationToken));
            }

            return exceptions;
        }

        private static void ForEachThreadLoop<T>(
            BlockingCollection<T> items,
            Action<T> handleItem,
            ConcurrentBag<Exception> exceptions,
            CancellationToken cancellationToken)
        {
            bool IsRunning()
            {
                var interrupted = cancellationToken.IsCancellationRequested || exceptions.Count > 0;
                return !interrupted && !items.IsCompleted;
            };

            while (IsRunning())
            {
                if (!items.TryTake(out var item))
                {
                    Thread.Yield();
                    continue;
                }

                try
                {
                    handleItem(item);
                }
                catch (OperationCanceledException)
                {
                    // ignore this one as OperationCanceledException will be added at the end of ParallelExecution.ForEach
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        }
    }
}
