using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GZipTest.Core.Parallels
{
    internal static class ForEachParallelExecution
    {
        public static ConcurrentBag<Exception> ForEach<T>(
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
                    // ReSharper disable once AccessToDisposedClosure - we wait for thread completion below
                    ForEachThreadLoop(
                        blockingItems,
                        handleItem,
                        exceptions,
                        cancellationToken);
                })
                {
                    Name = $"{nameof(ForEachParallelExecution)} [{index}]",
                })
                .ToList();

            threads.ForEach(t => t.Start());

            blockingItems.AddRangeUntilCanceled(items, cancellationToken);
            blockingItems.CompleteAdding();

            threads.ForEach(t => t.Join());

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
            }

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
                    when (cancellationToken.IsCancellationRequested)
                {
                    // ignore this one
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        }
    }
}
