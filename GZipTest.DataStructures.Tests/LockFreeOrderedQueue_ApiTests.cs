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
        public void Enqueue_sorts_elements(int[] values)
        {
            var qrderedQueue = new LockFreeOrderedQueue<int>((x, y) => x.CompareTo(y));

            values.ForEach(qrderedQueue.Enqueue);

            var expected = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Assert.Equal(expected, qrderedQueue.ToArray());
        }

        [Fact]
        public void Enqueue_uses_stable_sort()
        {
            var qrderedQueue = new LockFreeOrderedQueue<(int Id, string Name)>(
                (x, y) => x.Id.CompareTo(y.Id)
            );

            var inputs = new[]
            {
                (3, "d"),
                (2, "b"),
                (1, "a"),
                (2, "c"),
            };

            inputs.ForEach(qrderedQueue.Enqueue);

            var expected = new[]
            {
                (1, "a"),
                (2, "b"),
                (2, "c"),
                (3, "d"),
            };
            Assert.Equal(expected, qrderedQueue.ToArray());
        }
    }
}
