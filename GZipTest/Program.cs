using System.Collections.Concurrent;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using GZipTest.DataStructures;

namespace GZipTest
{
    class Program
    {
        //TODO: not used
        static SemaphoreSlim Semaphore = new SemaphoreSlim(initialCount: 0);

        static void Main(string[] args)
        {
            // Console.CancelKeyPress += ConsoleCancelKeyPress;
            // new CollectionDemo().ApiTest();

            var degreeOfParallelism = new DegreeOfParallelism(3); //Environment.ProcessorCount - 2); // MainThread + QueueWorkerThread
            Console.WriteLine($"The number of processors on this computer is {Environment.ProcessorCount}.");
            Console.WriteLine($"The number of parallel workers is {degreeOfParallelism.Value}.");

            // var sourceFilepath = "./TestFiles/bigfile";
            // var chunkOfBytes = FileUtils.ReadBytes(sourceFilepath, 16 * NumberOfBytesIn.MEGABYTE);

            var items = Enumerable.Range(0, 30).Select((value, index) =>
            {
                // emulate reading from disk
                Thread.Sleep(100);
                return new IndexedValue<int>(value, index);
            });


            // Tests.TestTpl(items);
            Console.WriteLine();
            // Tests.TestCustom(items, degreeOfParallelism, null);


            Run(items, degreeOfParallelism);
        }

        static void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            if (args.SpecialKey != ConsoleSpecialKey.ControlC)
            {
                return;
            }

            Console.WriteLine("Cancelling...");
            // args.Cancel = true;
            // zipper.Cancel();
        }

        static void Run<T>(IEnumerable<IndexedValue<T>> items, DegreeOfParallelism degreeOfParallelism, CancellationToken cancellationToken = default)
        {
            IOrderedQueue<IndexedValue<T>> orderedQueue =
                new LockFreeOrderedQueue<IndexedValue<T>>((x, y) => x.Index.CompareTo(y.Index));

            var enqueuingCompleted = false;

            // var queueWorker = new Thread(() =>
            // {
            //     HandleOrderedQueue(orderedQueue, () => enqueuingCompleted, cancellationToken);
            // });
            // queueWorker.Start();

            var encodingExceptions = ParallelExecution.ForEach(
                items,
                handleItem: item =>
                {
                    if (item.Index == 7) { throw new Exception("T_T"); }
                    Console.WriteLine($"{Thread.CurrentThread.Name}: starts working on item {item.Index}");

                    // emulate encoding work
                    Thread.Sleep(300);

                    // Console.WriteLine($"{Thread.CurrentThread.Name}: ends working on item {item.Index}");
                    orderedQueue.Enqueue(item);
                    //Semaphore.Release();
                },
                degreeOfParallelism,
                cancellationToken);

            enqueuingCompleted = true;

            if (encodingExceptions.Count > 0)
            {
                LogEncodingExceptions(encodingExceptions);
            }
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
            // bool IsRunning()
            // {
            //     var stoped = !cancellationToken.IsCancellationRequested;
            //     return !cancellationToken.IsCancellationRequested
            //         || !enqueuingCompleted()
            //         && orderedQueue.Count > 0;
            // };

            // saving chunks to FS
            var currentIndex = 0;
            // var wait = new SpinWait();
            while (!enqueuingCompleted() || orderedQueue.Count > 0)
            {
                var enqueuingCompleted_ = enqueuingCompleted();
                // Semaphore.Wait(cancellationToken);
                // if (!orderedQueue.TryPeek(out var pickedItem))
                // {
                //     // Console.WriteLine($"Could not peek item");
                //     // wait.SpinOnce();
                //     // Thread.Sleep(0);
                //     Thread.Sleep(100);
                //     continue;
                // }

                // wait.Reset();

                // if (pickedItem.Index != currentIndex)
                // {
                //     Console.WriteLine($"Peeked item with wrong index {pickedItem.Index}");
                //     continue;
                // }

                if (!orderedQueue.TryDequeue(out var item))
                {
                    Thread.Sleep(100);
                    //Console.WriteLine($"Could not dequeue already peeked item {pickedItem.Index}");
                    continue;
                }

                // if (orderedQueue.TryPeek(out var pickedItem) &&
                //    pickedItem.Index == currentIndex &&
                //    orderedQueue.TryDequeue(out var item))
                // {
                //     Console.WriteLine($"Writing to FS item {item.Index}");
                // }

                Console.WriteLine($"           Writing to FS item {item.Index} enqueuingCompleted: {enqueuingCompleted_}");
                currentIndex++;

                // Thread.Sleep(100);
            }
        }
    }
}
