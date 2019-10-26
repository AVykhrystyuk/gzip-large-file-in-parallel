using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GZipTest
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this BlockingCollection<T> self, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                self.Add(item);
            }
        }
    }
}
