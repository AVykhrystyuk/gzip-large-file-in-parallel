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
            var iterationExceptions = new ConcurrentBag<Exception>();

            using var blockingItems = new BlockingCollection<T>();
            var threads = Enumerable
                .Range(0, threadCount)
                .Select(index => new Thread(() =>
                {
                    ForEachThreadLoop(
                        blockingItems,
                        handleItem,
                        iterationExceptions,
                        cancellationToken);
                })
                {
                    Name = $"{nameof(ParallelExecution)} [{index}]",
                })
                .ToList();

            threads.ForEach(t => t.Start());

            blockingItems.AddRange(items);
            blockingItems.CompleteAdding();

            threads.ForEach(t => t.Join());

            return iterationExceptions;
        }

        [Obsolete("Async version is harder to maintain")]
        public static void ForEachAsync<T>(
            IEnumerable<T> items,
            Action<T> handleItem,
            Action<IReadOnlyCollection<Exception>>? completion = null,
            DegreeOfParallelism? degreeOfParallelism = default,
            CancellationToken cancellationToken = default)
        {
            var threadCount = (degreeOfParallelism ?? DegreeOfParallelism.Default).Value;
            var completedThreadCount = 0;
            var blockingItems = new BlockingCollection<T>();
            var iterationExceptions = new ConcurrentBag<Exception>();

            var threads = Enumerable
                .Range(0, threadCount)
                .Select(index => new Thread(() =>
                {
                    ForEachThreadLoop(
                        blockingItems,
                        handleItem,
                        iterationExceptions,
                        cancellationToken);

                    var allThreadsCompleted = Interlocked.Increment(ref completedThreadCount) == threadCount;
                    if (allThreadsCompleted)
                    {
                        var canceled = cancellationToken.IsCancellationRequested || iterationExceptions.Count > 0;
                        if (canceled)
                        {
                            blockingItems.CompleteAdding();
                        }
                        else
                        {
                            blockingItems.Dispose();
                            Console.WriteLine($"Thread '{Thread.CurrentThread.Name}' disposed shared blockingCollection");
                        }

                        completion?.Invoke(iterationExceptions);
                    }
                })
                {
                    Name = $"{nameof(ParallelExecution)} [{index}]",
                })
                .ToList();

            threads.ForEach(t => t.Start());

            var canceled = blockingItems.AddRange(items, cancellationToken);
            if (canceled)
            {
                blockingItems.Dispose();
                Console.WriteLine($"Thread '{Thread.CurrentThread.Name}' disposed shared blockingCollection");
            }
            else
            {
                blockingItems.CompleteAdding();
            }
            //threads.ForEach(t => t.Join());
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
                if (items.TryTake(out var item))
                {
                    try
                    {
                        handleItem(item);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }

                // Thread.Sleep(0);
            }
        }
    }
}
