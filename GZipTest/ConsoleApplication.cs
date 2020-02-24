using System;
using System.Threading;

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
            Console.CancelKeyPress += ConsoleCancelKeyPress;

            try
            {
                var command = this.commandParser.Parse(args);
                var validatedCommand = this.commandValidator.Validate(command);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }
            finally
            {
                Console.CancelKeyPress -= ConsoleCancelKeyPress;
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
