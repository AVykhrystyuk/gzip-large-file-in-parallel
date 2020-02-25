using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using GZipTest.DataStructures;

namespace GZipTest.Core
{
    public class ProgramCore
    {
        public static void Main(string[] args)
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

            var items = ranges;
                // .Select((value, index) =>
                // {
                //     // if (index == 10) cancellationTokenSource.Cancel();

                //     // emulate reading from disk
                //     Thread.Sleep(100);
                //     return new IndexedValue<int>(index: value, value: index);
                // });
                // .OrderByDescending(i => i.Index);

            // ParallelTests.TestCustom(items, degreeOfParallelism);
            // Console.WriteLine();
            // ParallelTests.TestTpl(items);

            MapReduce(
                items, 
                mapFn: i => { 
                    // emulate encoding work
                    Thread.Sleep(300); //  * (item.Index % 2 == 0 ? 4 : 1));
                    return i;
                },
                reduceFn: i => {
                    // emulate some work
                    Thread.Sleep(300);
                },
                degreeOfParallelism,
                cancellationTokenSource.Token);
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

        public static void MapReduce<T, T2>(
            IEnumerable<T> items, 
            Func<T, T2> mapFn,
            Action<T2> reduceFn,
            DegreeOfParallelism degreeOfParallelism, 
            CancellationToken cancellationToken = default)
        {
            var indexedItems = items.Select((value, index) => new IndexedValue<T>(index, value));

            var itemEnqueuedSemaphore = new SemaphoreSlim(initialCount: 0);

            IOrderedQueue<IndexedValue<T2>> orderedQueue =
                new LockFreeOrderedQueue<IndexedValue<T2>>((x, y) => x.Index.CompareTo(y.Index));

            var enqueuingCompleted = false;

            using var consumerLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            using var producersLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var queueWorker = new Thread(() =>
            {
                try
                {
                    HandleOrderedQueue(orderedQueue, reduceFn, () => enqueuingCompleted, itemEnqueuedSemaphore, producersLinkedCancellationTokenSource.Token);
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
                indexedItems,
                handleItem: item =>
                {
                    // if (item.Index == 27) { throw new Exception("!!!ParallelExecution!!!T_T"); }
                    Console.WriteLine($"{Thread.CurrentThread.Name}: starts working on item {item.Index}");

                    var newItem = item.Map(mapFn);

                    // Console.WriteLine($"{Thread.CurrentThread.Name}: ends working on item {item.Index}");
                    orderedQueue.Enqueue(newItem);
                    itemEnqueuedSemaphore.Release();
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

        static void HandleOrderedQueue<T>(
            IOrderedQueue<IndexedValue<T>> orderedQueue, 
            Action<T> action, 
            Func<bool> enqueuingCompleted,
            SemaphoreSlim itemEnqueuedSemaphore,
            CancellationToken cancellationToken = default)
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
                itemEnqueuedSemaphore.Wait(cancellationToken);

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

                    action(item.Value);

                    lookingForIndex++;
                }
            }
        }
    }
}
