using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace GZipTest
{
    public class ParallelExecutor
    {
        public ParallelExecutor(int numberOfWorkers)
        {

        }

        public void Execute<T>(IEnumerable<T> items, Action<T> handler)
        {
            using (var blockingCollection = new BlockingCollection<T>())
            {
                var threads = Enumerable
                    .Range(0, 7)
                    .Select(index =>
                    {
                        return new Thread(context =>
                        {
                            while (!blockingCollection.IsAddingCompleted || blockingCollection.Count > 0)
                            {
                                if (blockingCollection.TryTake(out var item)) 
                                {
                                   handler(item);
                                }

                                Thread.Sleep(0);
                            }
                        })
                        {
                            Name = "Thread " + index,
                        };
                    })
                    .ToList();

                foreach (var thread in threads)
                {
                    thread.Start("hello");
                }

                foreach (var item in items)
                {
                    blockingCollection.Add(item);
                }

                blockingCollection.CompleteAdding();

                foreach (var thread in threads)
                {
                    thread.Join();
                }
            }
        }
    }
}