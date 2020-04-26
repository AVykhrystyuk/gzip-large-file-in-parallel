using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GZipTest.DataStructures;

namespace GZipTest.Core.Parallels
{
    internal static class MapReduceParallelExecution
    {
        private class OrderedQueueRef<T>
        {
            private volatile bool enqueuingCompleted;

            private readonly SemaphoreSlim itemEnqueuedSemaphore = new SemaphoreSlim(initialCount: 0);

            public OrderedQueueRef(Comparison<T> comparison)
            {
                this.Queue = new LockFreeOrderedQueue<T>(comparison);
            }

            public IOrderedQueue<T> Queue { get; }

            public bool EnqueuingCompleted => this.enqueuingCompleted;

            public void CompleteEnqueuing()
            {
                this.enqueuingCompleted = true;
            }

            /// <summary>
            ///  Waits for an item being added to the queue using <see cref="Enqueue"/>
            /// </summary>
            public void WaitForEnqueuedOnce(CancellationToken cancellationToken = default)
            {
                this.itemEnqueuedSemaphore.Wait(cancellationToken);
            }

            public void Enqueue(T item)
            {
                this.Queue.Enqueue(item);
                this.itemEnqueuedSemaphore.Release();
            }
        }

        public static ConcurrentBag<Exception> MapReduce<T, T2>(
            IEnumerable<T> items,
            Func<T, T2> mapper,
            Action<T2> reducer,
            DegreeOfParallelism degreeOfParallelism = default,
            CancellationToken cancellationToken = default)
        {
            var indexedItems = items.Select((value, index) => IndexedValue.Create(index, value));

            var queueRef = new OrderedQueueRef<IndexedValue<T2>>((x, y) => x.Index.CompareTo(y.Index));

            using var consumerCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            using var producersCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var exceptions = new ConcurrentBag<Exception>();

            var queueWorkerThread = RunQueueWorkerThread(
                reducer,
                queueRef,
                handleException: ex =>
                {
                    // ReSharper disable once AccessToDisposedClosure - we wait for thread completion below
                    consumerCancellationTokenSource.Cancel();
                    exceptions.Add(ex);
                },
                cancellationToken: producersCancellationTokenSource.Token);

            var forEachExceptions = ForEachParallelExecution.ForEach(
                indexedItems,
                handleItem: item =>
                {
                    // if (item.Index == 27) { throw new Exception("!!!ParallelExecution!!!T_T"); }
                    Console.WriteLine($"{Thread.CurrentThread.Name}: starts working on item {item.Index}");

                    var newItem = item.Map(mapper);

                    // Console.WriteLine($"{Thread.CurrentThread.Name}: ends working on item {item.Index}");
                    queueRef.Enqueue(newItem);
                },
                degreeOfParallelism,
                cancellationToken: consumerCancellationTokenSource.Token);

            queueRef.CompleteEnqueuing();

            if (forEachExceptions.Count > 0)
            {
                producersCancellationTokenSource.Cancel();
            }

            exceptions.AddRange(forEachExceptions);

            queueWorkerThread.Join();

            return exceptions;
        }

        private static Thread RunQueueWorkerThread<T>(
            Action<T> reducer,
            OrderedQueueRef<IndexedValue<T>> queueRef,
            Action<Exception> handleException,
            CancellationToken cancellationToken)
        {
            var queueWorker = new Thread(() =>
            {
                try
                {
                    HandleOrderedQueue(queueRef, reducer, cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    // ignore this one
                }
                catch (Exception ex)
                {
                    handleException(ex);
                }
            });
            queueWorker.Start();
            return queueWorker;
        }

        private static void HandleOrderedQueue<T>(
            OrderedQueueRef<IndexedValue<T>> queueRef,
            Action<T> action,
            CancellationToken cancellationToken = default)
        {
            var orderedQueue = queueRef.Queue;
            bool IsRunning()
            {
                if (!queueRef.EnqueuingCompleted)
                {
                    return true;
                }

                return orderedQueue.Count > 0;
            }

            var lookingForIndex = 0;
            while (IsRunning())
            {
                // if (lookingForIndex == 3) throw new Exception("!!!HandleOrderedQueue!!!");

                queueRef.WaitForEnqueuedOnce(cancellationToken);

                while (orderedQueue.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!orderedQueue.TryPeek(out var pickedItem))
                    {
                        throw new InvalidOperationException("Could not peek an item");
                    }

                    if (pickedItem.Index != lookingForIndex)
                    {
                        Console.WriteLine(
                            $"                                                      [wrong-index] Peeked an item with index {pickedItem.Index}, but looking for an item with index {lookingForIndex}");
                        break;
                    }

                    if (!orderedQueue.TryDequeue(out var item))
                    {
                        throw new InvalidOperationException(
                            $"Could not dequeue already peeked item {pickedItem.Index}");
                    }

                    Console.WriteLine($"           Writing to FS item {item.Index}");

                    action(item.Value);

                    lookingForIndex++;
                }
            }
        }
    }
}
