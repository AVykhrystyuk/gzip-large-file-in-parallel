using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace GZipTest.DataStructures
{
    //TODO: Implement it properly, now it is just for POC
    public class LockFreeOrderedQueue<T> : IOrderedQueue<T>
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

        public LockFreeOrderedQueue(Comparison<T> comparison)
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
            item = default(T)!;
            if (this.list.Count < 1)
            {
                return false;
            }

            item = this.list[0];
            // this.list.RemoveAt(0);
            return true;
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
