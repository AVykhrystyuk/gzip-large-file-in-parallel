using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using GZipTest.Core.Parallels;

namespace GZipTest.Core
{
    public static class ProgramCore
    {
        private static readonly Random Random = new Random();

        public static void Main(string[] args)
        {
            // new CollectionDemo().ApiTest();

            var degreeOfParallelism = new DegreeOfParallelism(3); //Environment.ProcessorCount - 2); // MainThread + QueueWorkerThread
            Console.WriteLine($"The number of processors on this computer is {Environment.ProcessorCount}.");
            Console.WriteLine($"The number of parallel workers is {degreeOfParallelism.Value}.");

            // var sourceFilepath = "./TestFiles/bigfile";
            // var chunkOfBytes = FileUtils.ReadBytes(sourceFilepath, 16 * NumberOfBytesIn.MEGABYTE);

            using var cancellationTokenSource = new CancellationTokenSource();

            void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs cancelEventArgs)
            {
                if (cancelEventArgs.SpecialKey != ConsoleSpecialKey.ControlC)
                {
                    return;
                }

                Console.WriteLine(Environment.NewLine + "Cancelling...");
                cancelEventArgs.Cancel = true;

                cancellationTokenSource.Cancel();
            }

            Console.CancelKeyPress += ConsoleCancelKeyPress;

            var items =
                Enumerable.Range(0, 30)
                    // Enumerable.Empty<int>()
                    //     .Union(Enumerable.Range(0, 2).OrderByDescending(i => i))
                    //     .Union(Enumerable.Range(2, 2))
                    //     .Union(Enumerable.Range(4, 26).OrderByDescending(i => i))
                    .ToList();

            // ParallelTests.TestCustom(items, degreeOfParallelism);
            // Console.WriteLine();
            // ParallelTests.TestTpl(items);

            var exceptions = ParallelExecution.MapReduce(
                items,
                mapper: item =>
                {
                    // emulate encoding work
                    // if (item == 15) throw new Exception("!!!___ParallelExecution-ERROR___!!!");
                    Console.WriteLine($"{Thread.CurrentThread.Name}: starts working on item {item}");

                    Thread.Sleep(300
                        // * (item < 2 ? 5 : 1)
                        * Random.Next(1, 4)
                    );
                    return item;
                },
                reducer: item =>
                {
                    // if (item == 3) throw new Exception("!!!___ParallelExecution-ERROR___!!!");
                    Console.WriteLine($"           Writing to FS item {item}");
                    // emulate some work
                    Thread.Sleep(300);
                },
                degreeOfParallelism,
                cancellationTokenSource.Token);

            if (exceptions.Count > 0)
            {
                LogEncodingExceptions(exceptions);
            }
            else
            {
                Console.WriteLine($"All {items.Count} items are successfully reduced");
            }
        }

        private static void LogEncodingExceptions(IReadOnlyCollection<Exception> exceptions)
        {
            var errorMessageLines = new List<string>
            {
                $"The following exception(s) happened during {nameof(ParallelExecution)}:"
            };

            errorMessageLines.AddRange(exceptions.Select(e => e.ToString()));
            var errorMessage = string.Join(Environment.NewLine, errorMessageLines);
            Console.WriteLine(errorMessage);
        }
    }
}
