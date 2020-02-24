using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest
{
    public static class CollectionExtensions
    {
        public static bool AddRangeSafe<T>(this BlockingCollection<T> collection, IEnumerable<T> items, CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                collection.Add(item);
            }

            return true;
        }
    }
}
