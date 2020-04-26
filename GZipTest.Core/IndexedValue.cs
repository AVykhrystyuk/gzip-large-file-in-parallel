using System;
using System.Diagnostics;

namespace GZipTest.Core
{
    [DebuggerDisplay("Index = {Index} |  Value = {Value}")]
    public class IndexedValue<T>
    {
        public IndexedValue(int index, T value)
        {
            this.Index = index;
            this.Value = value;
        }

        public int Index { get; }
        public T Value { get; }

        public IndexedValue<TOut> Map<TOut>(Func<T, TOut> mapper) => IndexedValue.Create(
            this.Index,
            value: mapper(this.Value));
    }

    public static class IndexedValue
    {
        // Allows to omit specifying a generic parameter deriving it from passed value
        public static IndexedValue<T> Create<T>(int index, T value)
        {
            return new IndexedValue<T>(index, value);
        }
    }
}
