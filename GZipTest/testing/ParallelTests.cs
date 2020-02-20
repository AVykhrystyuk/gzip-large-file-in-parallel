using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GZipTest
{
    [Obsolete]
    public static class ParallelTests
    {
        static void TestCustom<T>(IEnumerable<IndexedValue<T>> items, DegreeOfParallelism degreeOfParallelism)
        {
            Console.WriteLine("Start testing Custom");

            var sw = Stopwatch.StartNew();

            ParallelExecution.ForEach(items, item =>
            {
                Console.WriteLine($"{Thread.CurrentThread.Name} : takes item {item.Index}");
                Thread.Sleep(200);
            }, degreeOfParallelism);

            sw.Stop();
            Console.WriteLine("End testing Custom");
            Console.WriteLine($"ms: {sw.ElapsedMilliseconds}, ticks: {sw.ElapsedTicks}");
        }

        static void TestTpl<T>(IEnumerable<IndexedValue<T>> items)
        {
            Console.WriteLine("Start testing Tpl");

            var sw = Stopwatch.StartNew();

            Parallel.ForEach(items, item =>
            {
                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} : takes item {item.Index}");
                Thread.Sleep(200);
            });

            sw.Stop();
            Console.WriteLine("End testing Tpl");
            Console.WriteLine($"ms: {sw.ElapsedMilliseconds}, ticks: {sw.ElapsedTicks}");
        }
    }
}
