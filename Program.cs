using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace GZipTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var numberOfWorkers = Math.Max(Environment.ProcessorCount - 1, 1);
            Console.WriteLine($"The number of processors on this computer is {Environment.ProcessorCount}.");
            Console.WriteLine($"The number of parallel workers is {numberOfWorkers}.");
            
            var parallelExecutor = new ParallelExecutor(numberOfWorkers);

            // var sourceFilepath = "./TestFiles/bigfile";;
            // var chunkOfBytes = FileUtils.ReadBytes(sourceFilepath, NumberOfBytesIn.MEGABYTE);
            
            var values = Enumerable.Range(0, 10).Select(n => {
                Thread.Sleep(100);
                return n;
            });

            
            TestTpl(values);
            Console.WriteLine();
            TestCustom(values, parallelExecutor);
        }

        static void TestCustom<T>(IEnumerable<T> items, ParallelExecutor parallelExecutor) 
        {
            Console.WriteLine("Start testing Custom");

            var sw = Stopwatch.StartNew();
            
            var countOfReads = 0;
            parallelExecutor.Execute(items, item =>
            { 
                Console.WriteLine(Thread.CurrentThread.Name + ": takes item " + item);
                Interlocked.Increment(ref countOfReads);
                Thread.Sleep(200);
            });

            sw.Stop();
            Console.WriteLine("End testing Custom");
            Console.WriteLine($"Count of reads: {countOfReads}");
            Console.WriteLine($"ms: {sw.ElapsedMilliseconds}, ticks: {sw.ElapsedTicks}");
        }

        static void TestTpl<T>(IEnumerable<T> items) 
        {
            Console.WriteLine("Start testing Tpl");

            var sw = Stopwatch.StartNew();
            
            var countOfReads = 0;
            Parallel.ForEach(items, item =>
            {
                Console.WriteLine(Thread.CurrentThread.ManagedThreadId + ": takes item " + item);
                Interlocked.Increment(ref countOfReads);
                Thread.Sleep(200);
            });

            sw.Stop();
            Console.WriteLine("End testing Tpl");
            Console.WriteLine($"Count of reads: {countOfReads}");
            Console.WriteLine($"ms: {sw.ElapsedMilliseconds}, ticks: {sw.ElapsedTicks}");
        }
    }
}
