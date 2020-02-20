using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest
{
    public static class CollectionExtensions
    {
        [Obsolete]
        public static bool AddRange<T>(this BlockingCollection<T> collection, IEnumerable<T> items, CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                if (cancellationToken.IsCancellationRequested || collection.IsAddingCompleted)
                {
                    return true;
                }

                try
                {
                    collection.Add(item);
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool AddRange<T>(this BlockingCollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                try
                {
                    collection.Add(item);
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            }

            return false;
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }
        }
    }
}
