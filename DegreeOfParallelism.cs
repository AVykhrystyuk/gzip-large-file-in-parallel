using System;

namespace GZipTest
{
    public class DegreeOfParallelism
    {
        public static readonly DegreeOfParallelism Default = new DegreeOfParallelism();

        //
        // Summary:
        //     max degree of parallelism
        //
        private const int MAXDOP = 512;

        public int Value { get; }

        public DegreeOfParallelism()
            : this(System.Environment.ProcessorCount)
        {

        }

        public DegreeOfParallelism(int value)
        {
            this.Value = NormalizeValue(value);
        }

        private static int NormalizeValue(int value)
        {
            return Math.Max(
                Math.Min(value, MAXDOP),
                1
            );
        }
    }
}
