using System;
using System.Linq;

namespace GZipTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourceFilepath = "./TestFiles/bigfile";
            var chunkOfBytes = FileUtils.ReadBytes(sourceFilepath, NumberOfBytesIn.MEGABYTE);
            var fileSize = chunkOfBytes.Sum(bytes => bytes.Length);
            Console.WriteLine($"FileSize: {fileSize}");
        }
    }
}
