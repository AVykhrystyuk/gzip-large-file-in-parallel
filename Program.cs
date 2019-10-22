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
            var degreeOfParallelism = new DegreeOfParallelism(Environment.ProcessorCount - 1);
            Console.WriteLine($"The number of processors on this computer is {Environment.ProcessorCount}.");
            Console.WriteLine($"The number of parallel workers is {degreeOfParallelism.Value}.");

            // var sourceFilepath = "./TestFiles/bigfile";
            // var chunkOfBytes = FileUtils.ReadBytes(sourceFilepath, NumberOfBytesIn.MEGABYTE);

            var workItems = Enumerable.Range(0, 10).Select(n =>
            {
                Thread.Sleep(100);
                return n;
            });

            TestTpl(workItems);
            Console.WriteLine();
            TestCustom(workItems, degreeOfParallelism);
        }

        static void TestCustom<T>(IEnumerable<T> items, DegreeOfParallelism degreeOfParallelism)
        {
            Console.WriteLine("Start testing Custom");

            var sw = Stopwatch.StartNew();

            ParallelExecution.ForEach(items, item =>
            {
                Console.WriteLine($"{Thread.CurrentThread.Name} : takes item {item}");
                Thread.Sleep(200);
            }, degreeOfParallelism);

            sw.Stop();
            Console.WriteLine("End testing Custom");
            Console.WriteLine($"ms: {sw.ElapsedMilliseconds}, ticks: {sw.ElapsedTicks}");
        }

        static void TestTpl<T>(IEnumerable<T> items)
        {
            Console.WriteLine("Start testing Tpl");

            var sw = Stopwatch.StartNew();

            Parallel.ForEach(items, item =>
            {
                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} : takes item {item}");
                Thread.Sleep(200);
            });

            sw.Stop();
            Console.WriteLine("End testing Tpl");
            Console.WriteLine($"ms: {sw.ElapsedMilliseconds}, ticks: {sw.ElapsedTicks}");
        }
    }
}
