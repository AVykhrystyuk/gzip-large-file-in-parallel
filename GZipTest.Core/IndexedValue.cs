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

        public IndexedValue<TOut> Map<TOut>(Func<T, TOut> mapFn) => new IndexedValue<TOut>(
            this.Index, 
            mapFn(this.Value));
    }
}
