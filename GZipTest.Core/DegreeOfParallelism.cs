using System;

namespace GZipTest.Core
{
    public class DegreeOfParallelism
    {
        public static readonly DegreeOfParallelism Default = new DegreeOfParallelism(Environment.ProcessorCount);

        /// <summary>
        /// Max degree of parallelism
        /// </summary>
        private const int MAXDOP = 512;

        public int Value { get; }

        public DegreeOfParallelism(int value)
        {
            this.Value = NormalizeValue(value, min: 1, max: MAXDOP);
        }

        private static int NormalizeValue(int value, int min, int max)
        {
            return Math.Max(
                Math.Min(value, max),
                min);
        }
    }
}
