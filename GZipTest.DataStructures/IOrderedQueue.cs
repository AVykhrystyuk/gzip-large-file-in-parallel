using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GZipTest.DataStructures
{
    public interface IOrderedQueue<T> : IEnumerable<T>
    {
        int Count { get; }

        void Enqueue(T item);

        bool TryDequeue([MaybeNullWhen(false)] out T item);

        bool TryPeek([MaybeNullWhen(false)] out T item);
    }
}
