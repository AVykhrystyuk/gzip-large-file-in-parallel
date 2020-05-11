using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest.Core
{
    public static class CollectionExtensions
    {
        public static bool AddRangeUntilCanceled<T>(this BlockingCollection<T> collection, IEnumerable<T> items, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                // ReSharper disable once MethodSupportsCancellation - that method throws exception and we don't need it here
                collection.Add(item);
            }

            return true;
        }

        public static void AddRange<T>(this ConcurrentBag<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }
    }
}
