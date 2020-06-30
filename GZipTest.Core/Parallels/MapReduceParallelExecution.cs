using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using GZipTest.DataStructures;

namespace GZipTest.Core.Parallels
{
    internal static class MapReduceParallelExecution
    {
        private class BlockingOrderedQueue<T> : IDisposable
        {
            private volatile bool enqueuingCompleted;

            private readonly SemaphoreSlim itemEnqueuedSemaphore = new SemaphoreSlim(initialCount: 0);
            private readonly CancellationTokenSource consumersCancellationTokenSource = new CancellationTokenSource();

            private readonly IOrderedQueue<T> queue;

            public BlockingOrderedQueue(Comparison<T> comparison, IOrderedQueue<T>? queue = null)
            {
                this.queue = queue ?? new LockFreeOrderedQueue<T>(comparison); //new POC_PoorlyImplemented_ConcurrentOrderedQueue<T>(comparison);
            }

            public int Count => this.queue.Count;

            public bool EnqueuingCompleted => this.enqueuingCompleted;

            public void CompleteEnqueuing()
            {
                this.enqueuingCompleted = true;
                this.CancelWaitingConsumers();
            }

            /// <summary>
            ///  Waits for an item being added to the queue using <see cref="BlockingOrderedQueue.Enqueue(T)"/>
            /// </summary>
            public bool WaitForEnqueuedOnce(CancellationToken cancellationToken = default)
            {
                var waitNeeded = !this.itemEnqueuedSemaphore.Wait(0);
                if (waitNeeded)
                {
                    // to stop waiting when CompleteEnqueuing() is called
                    using var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        this.consumersCancellationTokenSource.Token);

                    try
                    {
                        this.itemEnqueuedSemaphore.Wait(combinedTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Does not really mater if someone canceled the while operation or if CompleteEnqueuing()
                        // was called previously. Thus, no need to throw an exception here.
                        // cancellationToken.ThrowIfCancellationRequested();
                        return false;
                    }
                }

                return true;
            }

            public void AllowToWaitForEnqueuedOnceMore() =>
                this.itemEnqueuedSemaphore.Release();

            public void Enqueue(T item)
            {
                this.queue.Enqueue(item);
                this.itemEnqueuedSemaphore.Release();
            }

            public bool TryDequeue([MaybeNullWhen(false)] out T item) =>
                this.queue.TryDequeue(out item);

            public bool TryPeek([MaybeNullWhen(false)] out T item) =>
                this.queue.TryPeek(out item);

            public void Dispose()
            {
                this.itemEnqueuedSemaphore.Dispose();
                this.consumersCancellationTokenSource.Dispose();
            }

            private void CancelWaitingConsumers() =>
                this.consumersCancellationTokenSource.Cancel();
        }

        public static ConcurrentBag<Exception> MapReduce<T, T2>(
            IEnumerable<T> items,
            Func<T, T2> mapper,
            Action<T2> reducer,
            DegreeOfParallelism? degreeOfParallelism = default,
            CancellationToken cancellationToken = default)
        {
            var indexedItems = items.Select((value, index) => IndexedValue.Create(index, value));
            using var orderedQueue = new BlockingOrderedQueue<IndexedValue<T2>>((x, y) => x.Index.CompareTo(y.Index));
            using var consumerCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            using var producersCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var exceptions = new ConcurrentBag<Exception>();

            var queueWorkerThread = RunQueueWorkerThread(
                reducer,
                orderedQueue,
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
                    var mappedItem = item.Map(mapper);
                    orderedQueue.Enqueue(mappedItem);
                },
                degreeOfParallelism,
                cancellationToken: consumerCancellationTokenSource.Token);

            orderedQueue.CompleteEnqueuing();

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
            BlockingOrderedQueue<IndexedValue<T>> orderedQueue,
            Action<Exception> handleException,
            CancellationToken cancellationToken)
        {
            var queueWorker = new Thread(() =>
            {
                try
                {
                    HandleOrderedQueue(orderedQueue, reducer, cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    // ignore this one as it is handled in ParallelExecution facade
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
            BlockingOrderedQueue<IndexedValue<T>> orderedQueue,
            Action<T> action,
            CancellationToken cancellationToken = default)
        {
            var lookingForIndex = 0;
            while (!orderedQueue.EnqueuingCompleted || orderedQueue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var waitWasSuccessfulOrItemWasAlreadyEnqueued = orderedQueue.WaitForEnqueuedOnce(cancellationToken);
                if (!waitWasSuccessfulOrItemWasAlreadyEnqueued && orderedQueue.EnqueuingCompleted)
                {
                    // If we got here then CompleteEnqueuing() was called previously.
                    // Thus, no more item will be enqueued
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (!orderedQueue.TryPeek(out var pickedItem))
                {
                    throw new InvalidOperationException("Could not peek an item");
                }

                if (pickedItem.Index != lookingForIndex)
                {
                    // Console.WriteLine($"[wrong-index] Peeked an item with index {pickedItem.Index}, but was looking for an item with index {lookingForIndex}");

                    orderedQueue.AllowToWaitForEnqueuedOnceMore();
                    continue;
                }

                if (!orderedQueue.TryDequeue(out var dequeuedItem))
                {
                    var errorMessage = $"Could not dequeue already peeked item {pickedItem.Index}";
                    throw new InvalidOperationException(errorMessage);
                }

                action(dequeuedItem.Value);

                lookingForIndex++;
            }
        }
    }
}
