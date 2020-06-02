using System;
using System.Linq;
using Xunit;

namespace GZipTest.DataStructures.Tests
{
    public class LockFreeOrderedQueue_ApiTests
    {
        [Theory]
        [InlineData(new[] { 4, 5, 8, 2, 7, 1, 3, 6, 0, 9 })]
        [InlineData(new[] { 1, 0, 2, 5, 7, 4, 3, 8, 9, 6 })]
        [InlineData(new[] { 8, 5, 2, 3, 4, 9, 7, 0, 6, 1 })]
        [InlineData(new[] { 5, 2, 4, 0, 3, 1, 8, 6, 7, 9 })]
        public void Enqueue_sorts_elements(int[] inputItems)
        {
            var orderedQueue = new LockFreeOrderedQueue<int>((x, y) => x.CompareTo(y));

            inputItems.ForEach(orderedQueue.Enqueue);

            var expectedItems = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            AssertItemsAreEqual(expectedItems, orderedQueue);
        }

        [Fact]
        public void Enqueue_uses_stable_sort()
        {
            // Arrange
            var orderedQueue = new LockFreeOrderedQueue<(int Id, string Name)>(
                (x, y) => x.Id.CompareTo(y.Id)
            );

            // Act
            new[]
            {
                (3, "d"),
                (2, "b"),
                (1, "a"),
                (2, "c"),
            }.ForEach(orderedQueue.Enqueue);

            // Assert
            var expectedItems = new[]
            {
                (1, "a"),
                (2, "b"),
                (2, "c"),
                (3, "d"),
            };
            AssertItemsAreEqual(expectedItems, orderedQueue);
        }

        [Theory]
        [InlineData(
            /* inputItems: */ new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
            /* numberOrDequeueCalls: */ 3,
            /* expectedItems: */ new[] { 3, 4, 5, 6, 7, 8, 9 })]
        [InlineData(
            /* inputItems: */ new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
            /* numberOrDequeueCalls: */ 6,
            /* expectedItems: */ new[] { 6, 7, 8, 9 })]
        [InlineData(
            /* inputItems: */ new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
            /* numberOrDequeueCalls: */ 10,
            /* expectedItems: */ new int[0])]
        public void TryDequeue_removes_elements(int[] inputItems, int numberOrDequeueCalls, int[] expectedItems)
        {
            // Arrange
            var orderedQueue = new LockFreeOrderedQueue<int>((x, y) => x.CompareTo(y));
            inputItems.ForEach(orderedQueue.Enqueue);

            // Act
            for (var i = 0; i < numberOrDequeueCalls; i++)
            {
                orderedQueue.TryDequeue(out _);
            }

            // Assert
            AssertItemsAreEqual(expectedItems, orderedQueue);
        }

        [Fact]
        public void TryPeek_fails_for_empty_queue()
        {
            var orderedQueue = new LockFreeOrderedQueue<int>((x, y) => x.CompareTo(y));

            var success = orderedQueue.TryPeek(out var emptyItem);

            Assert.False(success);
            Assert.Equal(default, emptyItem);
            Assert.Equal(0, orderedQueue.Count);
        }

        [Fact]
        public void TryPeek_always_picks_first_item_after_enqueuing()
        {
            var orderedQueue = new LockFreeOrderedQueue<int>((x, y) => x.CompareTo(y));

            var inputItems = Enumerable.Range(0, 10).ToArray();
            var firstItems = inputItems.First();

            foreach (var inputItem in inputItems)
            {
                orderedQueue.Enqueue(inputItem);

                var success = orderedQueue.TryPeek(out var pickedItem);

                Assert.True(success);
                Assert.Equal(firstItems, pickedItem);
            }
        }

        [Fact]
        public void TryPeek_always_picks_first_item_when_dequeuing()
        {
            var orderedQueue = new LockFreeOrderedQueue<int>((x, y) => x.CompareTo(y));

            const int numberOfItems = 10;
            Enumerable.Range(0, numberOfItems).ForEach(orderedQueue.Enqueue);

            for (var i = 0; i < numberOfItems; i++)
            {
                var success = orderedQueue.TryPeek(out var pickedItem);

                Assert.True(success);
                Assert.Equal(i, pickedItem);

                orderedQueue.TryDequeue(out _);
            }
        }

        private static void AssertItemsAreEqual<T>(T[] expectedItems, LockFreeOrderedQueue<T> orderedQueue)
        {
            Assert.Equal(expectedItems, orderedQueue.ToArray());
            Assert.Equal(expectedItems.Length, orderedQueue.Count);
        }
    }
}
