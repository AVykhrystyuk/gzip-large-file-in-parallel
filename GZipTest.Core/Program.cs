using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using GZipTest.DataStructures;
using Microsoft.Extensions.DependencyInjection;

namespace GZipTest.Core
{
    class Program
    {
        //TODO: not used
        static SemaphoreSlim Semaphore = new SemaphoreSlim(initialCount: 0);

        static void Main(string[] args)
        {
            // new CollectionDemo().ApiTest();

            var degreeOfParallelism = new DegreeOfParallelism(3); //Environment.ProcessorCount - 2); // MainThread + QueueWorkerThread
            Console.WriteLine($"The number of processors on this computer is {Environment.ProcessorCount}.");
            Console.WriteLine($"The number of parallel workers is {degreeOfParallelism.Value}.");

            // var sourceFilepath = "./TestFiles/bigfile";
            // var chunkOfBytes = FileUtils.ReadBytes(sourceFilepath, 16 * NumberOfBytesIn.MEGABYTE);

            using var cancellationTokenSource = new CancellationTokenSource();

            void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs args)
            {
                if (args.SpecialKey != ConsoleSpecialKey.ControlC)
                {
                    return;
                }

                Console.WriteLine("Cancelling...");
                args.Cancel = true;

                cancellationTokenSource.Cancel();
            }
            Console.CancelKeyPress += ConsoleCancelKeyPress;

            var ranges =
                Enumerable.Range(0, 30)
            // Enumerable.Empty<int>()
            //     .Union(Enumerable.Range(0, 2).OrderByDescending(i => i))
            //     .Union(Enumerable.Range(2, 2))
            //     .Union(Enumerable.Range(4, 26).OrderByDescending(i => i))
                .ToList();

            var items = ranges
                .Select((value, index) =>
                {
                    // if (index == 10) cancellationTokenSource.Cancel();

                    // emulate reading from disk
                    Thread.Sleep(100);
                    return new IndexedValue<int>(index: value, value: index);
                });
                // .OrderByDescending(i => i.Index);

            // ParallelTests.TestCustom(items, degreeOfParallelism);
            // Console.WriteLine();
            // ParallelTests.TestTpl(items);

            Run(items, degreeOfParallelism, cancellationTokenSource.Token);
        }

        static void Run<T>(IEnumerable<IndexedValue<T>> items, DegreeOfParallelism degreeOfParallelism, CancellationToken cancellationToken = default)
        {
            IOrderedQueue<IndexedValue<T>> orderedQueue =
                new LockFreeOrderedQueue<IndexedValue<T>>((x, y) => x.Index.CompareTo(y.Index));

            var enqueuingCompleted = false;

            using var consumerLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            using var producersLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var queueWorker = new Thread(() =>
            {
                try
                {
                    HandleOrderedQueue(orderedQueue, () => enqueuingCompleted, producersLinkedCancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // ignore this one
                }
                catch (Exception)
                {
                    consumerLinkedCancellationTokenSource.Cancel();
                }
            });
            queueWorker.Start();

            var encodingExceptions = ParallelExecution.ForEach(
                items,
                handleItem: item =>
                {
                    // if (item.Index == 27) { throw new Exception("!!!ParallelExecution!!!T_T"); }
                    Console.WriteLine($"{Thread.CurrentThread.Name}: starts working on item {item.Index}");

                    // emulate encoding work
                    Thread.Sleep(300); //  * (item.Index % 2 == 0 ? 4 : 1));

                    // Console.WriteLine($"{Thread.CurrentThread.Name}: ends working on item {item.Index}");
                    orderedQueue.Enqueue(item);
                    Semaphore.Release();
                },
                degreeOfParallelism,
                consumerLinkedCancellationTokenSource.Token);

            enqueuingCompleted = true;

            if (encodingExceptions.Count > 0)
            {
                if (!encodingExceptions.OfType<OperationCanceledException>().Any())
                {
                    producersLinkedCancellationTokenSource.Cancel();
                }

                LogEncodingExceptions(encodingExceptions);
            }

            queueWorker.Join();
        }

        static void LogEncodingExceptions(IReadOnlyCollection<Exception> exceptions)
        {
            var errorMessageLines = new List<string>
            {
                $"The following {exceptions.Count} exception(s) happened during ParallelEncoding:",
            };
            errorMessageLines.AddRange(exceptions.Select(e => e.ToString()));
            var errorMessage = string.Join(Environment.NewLine, errorMessageLines);
            Console.WriteLine(errorMessage);
        }

        static void HandleOrderedQueue<T>(IOrderedQueue<IndexedValue<T>> orderedQueue, Func<bool> enqueuingCompleted, CancellationToken cancellationToken = default)
        {
            bool IsRunning()
            {
                var queueCompleted = enqueuingCompleted() && orderedQueue.Count == 0;
                return !queueCompleted;
            };

            var lookingForIndex = 0;
            while (IsRunning())
            {
                // if (lookingForIndex == 3) throw new Exception("!!!HandleOrderedQueue!!!");

                // wait for an item being added to the queue
                Semaphore.Wait(cancellationToken);

                while (orderedQueue.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!orderedQueue.TryPeek(out var pickedItem))
                    {
                        throw new InvalidOperationException("Could not peek an item");
                    }

                    if (pickedItem.Index != lookingForIndex)
                    {
                        Console.WriteLine($"                                                      [wrong-index] Peeked an item with index {pickedItem.Index}, but looking for an item with index {lookingForIndex}");
                        break;
                    }

                    if (!orderedQueue.TryDequeue(out var item))
                    {
                        throw new InvalidOperationException($"Could not dequeue already peeked item {pickedItem.Index}");
                    }

                    Console.WriteLine($"           Writing to FS item {item.Index}");

                    // emulate some work
                    Thread.Sleep(300);

                    lookingForIndex++;
                }
            }
        }
    }
}
