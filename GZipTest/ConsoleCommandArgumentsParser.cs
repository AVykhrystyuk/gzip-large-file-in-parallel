using System;
using System.Linq;

namespace GZipTest
{
    public interface IConsoleCommandArgumentsParser
    {
        ConsoleCommandArguments Parse(string[] args);
    }

    public class ConsoleCommandArgumentsParser : IConsoleCommandArgumentsParser
    {
        private readonly string[] allowedCommands = new [] { "compress", "decompress" };

        public ConsoleCommandArguments Parse(string[] args)
        {
            if (args.Length < 3 ||
                !this.allowedCommands.Contains(args[0]))
            {
                throw new InvalidOperationException($"Invalid command.{Environment.NewLine}Supported commands format:{Environment.NewLine}GZipTest.exe compress/decompress [sourceFilePath] [destinationFilePath]");
            }

            return new ConsoleCommandArguments
            {
                Compress = this.allowedCommands[0] == args[0],
                SourcePath = args[1],
                DestinationPath = args[2],
            };
        }
    }
}
