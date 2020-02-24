using System.IO;

namespace GZipTest
{
    public class ConsoleCommandArguments
    {
        public bool Compress { get; set;}
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
    }

    public class ValidatedConsoleCommandArguments
    {
        public ValidatedConsoleCommandArguments(bool compress, FileInfo sourceFile, FileInfo destinationFile)
        {
            this.Compress = compress;
            this.SourceFile = sourceFile;
            this.DestinationFile = destinationFile;
        }

        public bool Compress { get; }
        public FileInfo SourceFile { get; }
        public FileInfo DestinationFile { get; }
    }
}
