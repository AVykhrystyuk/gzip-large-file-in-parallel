using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

// Ignore passing 'volatile' field as a 'ref' argument - used only in CAS operations
#pragma warning disable 420

namespace GZipTest.DataStructures
{
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public class LockFreeOrderedQueue<T> : IOrderedQueue<T>
    {
        [DebuggerDisplay("Removed = {" + nameof(Removed) + "}")]
        private class NodeNextRemovedState
        {
            private volatile Node? next;

            public NodeNextRemovedState(Node? next = null, bool removed = false)
            {
                this.next = next;
                this.Removed = removed;
            }

            public Node? Next => this.next;

            public bool Removed { get; }

            /// <summary>
            /// HACK: the class is logically immutable but this method allows to avoid extra memory allocation
            /// which can cause GC at an inappropriate time
            /// </summary>
            internal void MutateNextTo(Node? node)
            {
                this.next = node;
            }

            public NodeNextRemovedState WithNextTo(Node? nextNode)
            {
                return new NodeNextRemovedState(nextNode, this.Removed);
            }

            public NodeNextRemovedState WithRemoved()
            {
                return new NodeNextRemovedState(this.Next, removed: true);
            }
        }

        [DebuggerDisplay("Removed = {State.Removed}")]
        private class Node
        {
            public volatile NodeNextRemovedState State = new NodeNextRemovedState(next: null, removed: false);

            public Node(T item)
            {
                this.Item = item;
            }

            public T Item { get; }

            /// <summary>
            /// HACK: the <see cref="State"/> is logically immutable but this method allows to avoid extra memory allocation
            /// which can cause GC at an inappropriate time
            /// </summary>
            internal void __PointNextTo(Node? node)
            {
                this.State.MutateNextTo(node);
            }

            public bool CasState(NodeNextRemovedState oldState, NodeNextRemovedState newState)
            {
                return InterlockedExt.CAS(ref this.State, comparand: oldState, newState);
            }

            public static bool IsAlive([NotNullWhen(true)] Node? node, [NotNullWhen(true)] out NodeNextRemovedState? state)
            {
                /*
                if (node != null) {
                    state = node.State;
                    return !state.Removed;
                }
                state = null;
                return false;
                */

                state = node?.State;
                return state?.Removed == false;
            }
        }

        private readonly Comparison<T> comparison;

        private int count;

        private readonly Node fakeHead = new Node(default!);

        public int Count => this.count;

        public LockFreeOrderedQueue(Comparison<T> comparison)
        {
            this.comparison = comparison;
        }

        public void Enqueue(T item)
        {
            var newNode = new Node(item);
            var newNodeRefState = new NodeNextRemovedState(next: newNode);
            Node? startSearchAtNodeToInsertAfter = null;

            var spinWait = new SpinWait();
            do
            {
                var previousNode = this.FindNodeToInsertAfter(item, startSearchAtNodeToInsertAfter);
                var previousNodeState = previousNode.State;
                if (!previousNodeState.Removed)
                {
                    var currentNode = previousNodeState.Next;
                    newNode.__PointNextTo(currentNode);

                    if (previousNode.CasState(previousNodeState, newNodeRefState))
                    {
                        // ---previousNode         currentNode---
                        //                \       /
                        //                 newNode
                        break;
                    }
                }

                // ---previousNode---concurrentlyInsertedNode---currentNode---
                //                                               /
                //                                        newNode
                // or:
                // ---previousNode[removed]---currentNode---
                //                           /
                //                    newNode
                startSearchAtNodeToInsertAfter = previousNode;
                spinWait.SpinOnce();
            } while (true);

            Interlocked.Increment(ref this.count);
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T item)
        {
            //TODO: Test Dequeue in a row with [FakeHead] -> [removedNode] -> [aliveNode]
            var spinWait = new SpinWait();
            do
            {
                var fakeHeadState = this.fakeHead.State;

                var nodeToRemove = this.FindFirstAliveNode();
                // ---fakeHead---nodeToRemove---
                // or
                // ---fakeHead---node[removed]---nodeToRemove---

                if (!Node.IsAlive(nodeToRemove, out var nodeToRemoveState))
                {
                    item = default!;
                    return false;
                }

                if (!nodeToRemove.CasState(nodeToRemoveState, nodeToRemoveState.WithRemoved()))
                {
                    // ---nodeToRemove---concurrentlyInsertedNode---successorNode---
                    spinWait.SpinOnce();
                    continue;
                }

                // ---nodeToRemove[removed]---successorNode---

                item = nodeToRemove.Item;

                // Optimisation: Attempt to actually remove the node,
                // but that's not actually required since 'FindNodeToInsertAfter' also removes "dead" nodes
                var successorNode = nodeToRemoveState.Next;
                if (this.fakeHead.CasState(fakeHeadState, fakeHeadState.WithNextTo(successorNode)))
                {
                    // ---nodeToRemove[removed]---successorNode---
                    //                            /
                    //                    fakeHead
                }
                else
                {
                    // fakeHead---concurrentlyInsertedNode---nodeToRemove---
                }

                break;

            } while (true);

            Interlocked.Decrement(ref this.count);
            return true;
        }

        public bool TryPeek([MaybeNullWhen(false)] out T item)
        {
            var node = this.fakeHead.State.Next;
            while (node != null)
            {
                var nodeState = node.State;
                if (!nodeState.Removed)
                {
                    item = node.Item;
                    return true;
                }

                node = nodeState.Next;
            }

            item = default!;
            return false;
        }

        /// <summary>
        /// Finds the first alive node.
        /// Iterating through the sequence of nodes removing (from the sequence) the nodes that were marked as removed.
        /// </summary>
        private Node? FindFirstAliveNode()
        {
            var removedNodeOrFakeHead = this.FindLastSequentialNode(
                predicate: (_, nodeState) => nodeState.Removed);

            // ---removedNodeOrFakeHead(fakeHead)---aliveNode---
            // or
            // ---fakeHead---node[removed]---removedNodeOrFakeHead[removed]---aliveNode---
            return removedNodeOrFakeHead.State.Next;
        }

        /// <summary>
        /// Finds the last sequential node that satisfies the predicate.
        /// Iterating through the sequence of nodes removing (from the sequence) the nodes that were marked as removed.
        /// </summary>
        private Node FindLastSequentialNode(Func<T, NodeNextRemovedState, bool> predicate, Node? searchAt = null)
        {
            var spinWait = new SpinWait();
            do // loop to retry the whole operation
            {
                var retrySearchRequested = false;

                var previousNode = this.fakeHead;
                var previousNodeState = this.fakeHead.State;

                if (Node.IsAlive(searchAt, out var searchAtState))
                {
                    previousNode = searchAt;
                    previousNodeState = searchAtState;
                }

                var currentNode = previousNodeState.Next;
                while (currentNode != null) // && predicate(currentNode.Item, currentNode.State))
                {
                    var currentNodeState = currentNode.State;
                    if (!predicate(currentNode.Item, currentNodeState))
                    {
                        break;
                    }

                    var successorNode = currentNodeState.Next;

                    if (currentNodeState.Removed)
                    {
                        // ---previousNode---currentNode[removed]---successorNode
                        if (previousNode.CasState(previousNodeState, previousNodeState.WithNextTo(successorNode)))
                        {
                            // ---previousNode   currentNode[removed]---successorNode
                            //                \                        /
                            //                 ------------------------
                            currentNode = successorNode;
                            continue;
                        }

                        //TODO: review if we allow to use removed `searchAt` node, mb the following block is no longer needed then
                        var concurrentlyUpdatedPreviousNodeState = previousNode.State;
                        if (concurrentlyUpdatedPreviousNodeState.Removed)
                        {
                            // ---previousNode[removed]---currentNode[removed]---successorNode---
                            retrySearchRequested = true;
                            spinWait.SpinOnce();
                            break;
                        }

                        // ---previousNode---concurrentlyInsertedNode---currentNode[removed]---successorNode---
                        // or:
                        // ---previousNode   currentNode[removed]---successorNode---
                        //                \                        /
                        //                 ------------------------
                        currentNode = concurrentlyUpdatedPreviousNodeState.Next;
                        continue;
                    }

                    previousNode = currentNode;
                    previousNodeState = currentNodeState;
                    currentNode = successorNode;
                } // inner while loop

                if (!retrySearchRequested)
                {
                    return previousNode;
                }
            } while (true);
        }

        private Node FindNodeToInsertAfter(T item, Node? searchAt = null) =>
            this.FindLastSequentialNode(
                predicate: (nodeItem, _) => this.LessOrEqual(nodeItem, item),
                searchAt);

        private bool LessOrEqual(T item, T other) => this.comparison(item, other) <= 0;

        public IEnumerator<T> GetEnumerator()
        {
            var node = this.fakeHead.State.Next;
            while (node != null)
            {
                var nodeState = node.State;
                if (!nodeState.Removed)
                {
                    yield return node.Item;
                }

                node = nodeState.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    public class POC_PoorlyImplemented_ConcurrentOrderedQueue<T> : IOrderedQueue<T>
    {
        private readonly object lockObj = new object();
        private readonly List<T> list = new List<T>();
        private readonly Comparison<T> comparison;

        private int RealCount;

        public int Count
        {
            get
            {
                lock (this.lockObj)
                {
                    return this.list.Count;
                }
            }
        }

        public POC_PoorlyImplemented_ConcurrentOrderedQueue(Comparison<T> comparison)
        {
            this.comparison = comparison;
        }

        public void Enqueue(T item)
        {
            Interlocked.Increment(ref RealCount);

            lock (this.lockObj)
            {
                if (list.Count == 0)
                {
                    // add to head
                    this.list.Add(item);
                    return;
                }

                var lastIndex = -1;
                for (var i = 0; i < list.Count; i++)
                {
                    var x = list[i];
                    var indicator = this.comparison(x, item);
                    if (indicator > 0)
                    {
                        break;
                    }
                    lastIndex = i;
                }

                if (lastIndex == -1)
                {
                    // add to head
                    this.list.Insert(0, item);
                    return;
                }

                this.list.Insert(lastIndex + 1, item);
            }
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T item)
        {
            Interlocked.Decrement(ref RealCount);

            lock (this.lockObj)
            {
                if (this.list.Count == 0)
                {
                    item = default(T)!;
                    return false;
                }

                item = this.list[0];
                this.list.RemoveAt(0);
                return true;
            }
        }

        public bool TryPeek([MaybeNullWhen(false)] out T item)
        {
            lock (this.lockObj)
            {
                item = default(T)!;
                if (this.list.Count < 1)
                {
                    return false;
                }

                item = this.list[0];
                // this.list.RemoveAt(0);
                return true;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
