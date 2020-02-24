using System.IO;

namespace GZipTest
{
    public interface IConsoleCommandArgumentsValidator
    {
        ValidatedConsoleCommandArguments Validate(ConsoleCommandArguments command);
    }

    public class ConsoleCommandArgumentsValidator : IConsoleCommandArgumentsValidator
    {
        public ValidatedConsoleCommandArguments Validate(ConsoleCommandArguments command)
        {
            var sourceFile = new FileInfo(command.SourcePath);
            if (!sourceFile.Exists)
            {
                throw new FileNotFoundException("Could not find specified file.", fileName: command.SourcePath);
            }

            return new ValidatedConsoleCommandArguments(
                command.Compress,
                sourceFile,
                destinationFile: new FileInfo(command.DestinationPath));
        }
    }
}
