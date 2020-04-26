using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using GZipTest.Core.Parallels;

namespace GZipTest.Core
{
    public static class ProgramCore
    {
        public static void Main(string[] args)
        {
            // new CollectionDemo().ApiTest();

            var degreeOfParallelism = new DegreeOfParallelism(3); //Environment.ProcessorCount - 2); // MainThread + QueueWorkerThread
            Console.WriteLine($"The number of processors on this computer is {Environment.ProcessorCount}.");
            Console.WriteLine($"The number of parallel workers is {degreeOfParallelism.Value}.");

            // var sourceFilepath = "./TestFiles/bigfile";
            // var chunkOfBytes = FileUtils.ReadBytes(sourceFilepath, 16 * NumberOfBytesIn.MEGABYTE);

            using var cancellationTokenSource = new CancellationTokenSource();

            void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs args)
            {
                if (args.SpecialKey != ConsoleSpecialKey.ControlC)
                {
                    return;
                }

                Console.WriteLine("Cancelling...");
                args.Cancel = true;

                cancellationTokenSource.Cancel();
            }

            Console.CancelKeyPress += ConsoleCancelKeyPress;

            var ranges =
                Enumerable.Range(0, 30)
                    // Enumerable.Empty<int>()
                    //     .Union(Enumerable.Range(0, 2).OrderByDescending(i => i))
                    //     .Union(Enumerable.Range(2, 2))
                    //     .Union(Enumerable.Range(4, 26).OrderByDescending(i => i))
                    .ToList();

            var items = ranges;
            // .Select((value, index) =>
            // {
            //     // if (index == 10) cancellationTokenSource.Cancel();

            //     // emulate reading from disk
            //     Thread.Sleep(100);
            //     return new IndexedValue<int>(index: value, value: index);
            // });
            // .OrderByDescending(i => i.Index);

            // ParallelTests.TestCustom(items, degreeOfParallelism);
            // Console.WriteLine();
            // ParallelTests.TestTpl(items);


            var exceptions = ParallelExecution.MapReduce(
                items,
                mapper: i =>
                {
                    // emulate encoding work
                    Thread.Sleep(300); //  * (item.Index % 2 == 0 ? 4 : 1));
                    return i;
                },
                reducer: i =>
                {
                    // emulate some work
                    Thread.Sleep(300);
                },
                degreeOfParallelism,
                cancellationTokenSource.Token);

            if (exceptions.Count > 0)
            {
                LogEncodingExceptions(exceptions);
            }
        }

        private static void LogEncodingExceptions(IReadOnlyCollection<Exception> exceptions)
        {
            var errorMessageLines = new List<string>
            {
                $"The following exception(s) happened during ParallelEncoding:"
            };

            errorMessageLines.AddRange(exceptions.Select(e => e.ToString()));
            var errorMessage = string.Join(Environment.NewLine, errorMessageLines);
            Console.WriteLine(errorMessage);
        }
    }
}
