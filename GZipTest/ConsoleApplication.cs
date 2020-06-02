using System;
using System.Threading;
using GZipTest.Core;
using GZipTest.Core.Parallels;

namespace GZipTest
{
    public class ConsoleApplication : IDisposable
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly IConsoleCommandArgumentsParser commandParser;
        private readonly IConsoleCommandArgumentsValidator commandValidator;

        public ConsoleApplication(
            IConsoleCommandArgumentsParser commandParser,
            IConsoleCommandArgumentsValidator commandValidator)
        {
            this.commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
            this.commandValidator = commandValidator ?? throw new ArgumentNullException(nameof(commandValidator));
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Dispose();
        }

        public int Run(string[] args)
        {
            Console.CancelKeyPress += this.ConsoleCancelKeyPress;

            try
            {
                var validatedCommand = this.commandValidator.Validate(
                    this.commandParser.Parse(args));

                var degreeOfParallelism = new DegreeOfParallelism(3); //Environment.ProcessorCount - 2); // MainThread + QueueWorkerThread
                Console.WriteLine($"The number of processors on this computer is {Environment.ProcessorCount}.");
                Console.WriteLine($"The number of parallel workers is {degreeOfParallelism.Value}.");

                if (validatedCommand.Compress)
                {
                    new GZipEncoder().Encode(validatedCommand.SourceFile, validatedCommand.DestinationFile, degreeOfParallelism, this.cancellationTokenSource.Token);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }
            finally
            {
                Console.CancelKeyPress -= this.ConsoleCancelKeyPress;
            }
        }

        private void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            if (args.SpecialKey != ConsoleSpecialKey.ControlC)
            {
                return;
            }

            Console.WriteLine($"{Environment.NewLine}Cancelling...");
            args.Cancel = true;

            this.cancellationTokenSource.Cancel();
        }
    }
}
