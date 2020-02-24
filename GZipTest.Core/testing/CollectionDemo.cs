using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GZipTest.DataStructures;

namespace GZipTest.Core
{
    public class CollectionDemo
    {
        private readonly IOrderedQueue<IndexedValue<string>> orderedQueue
            = new LockFreeOrderedQueue<IndexedValue<string>>((x, y) => x.Index.CompareTo(y.Index));

        public void ApiTest()
        {
            // new Queue<int>().Peek
            var indexedItems = this.BuildRandomValues(start: 0, count: 10).ToList();
            this.PrintItems("Random", indexedItems);

            foreach (var item in indexedItems)
            {
                this.orderedQueue.Enqueue(item);
                this.PrintItems("LinkedList", this.orderedQueue);
            }

            Console.WriteLine("removing");

            var count = 15;
            while (count-- > 0)
            {
                if (this.orderedQueue.TryDequeue(out var item))
                {
                    this.PrintItems("LinkedList", this.orderedQueue);
                }
            }
        }

        private IEnumerable<IndexedValue<string>> BuildRandomValues(int start, int count)
        {
            // return "4,4,4,5,8,2,7,1,3,6,0,9".Split(',').Select(v => new IndexedValue<string>(int.Parse(v), $"v{v}"));

            return RandomUtils.GenerateNumbers(start, count)
                .Select((random, index) => new IndexedValue<string>(index: random, value: $"v{index}"));
        }

        private void PrintItems(string prefix, IEnumerable<IndexedValue<string>> items)
        {
            var line = string.Join(',', items.Select(x => x.Index));
            Console.WriteLine($"{prefix}: {line}");
        }
    }
}
