using System;
using System.Linq;

namespace GZipTest.Core
{
    public static class RandomUtils
    {
        /// <summary>
        /// Generates a sequence of random integral numbers within a specified range.
        /// </summary>
        public static int[] GenerateNumbers(int start, int count)
        {
            var random = new Random();

            var numbers = Enumerable.Range(start, count).ToArray();

            for (var i = 0; i < numbers.Length; ++i)
            {
                var randomIndex = random.Next(numbers.Length);

                var temp = numbers[randomIndex];
                numbers[randomIndex] = numbers[i];
                numbers[i] = temp;
            }

            return numbers;
        }
    }
}
