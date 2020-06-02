using System;
using System.IO;
using System.Linq;
using System.Threading;
using GZipTest.Core.Parallels;

namespace GZipTest.Core
{
    public class GZipEncoder
    {
        public void Encode(FileInfo sourceFile, FileInfo destinationFile, DegreeOfParallelism degreeOfParallelism, CancellationToken cancellationToken = default) 
        {
            if (destinationFile.Exists) 
            {
                destinationFile.Delete();
            }

            var byteChunks = FileUtils.ReadBytes(sourceFile.FullName, NumberOfBytesIn.MEGABYTE, cancellationToken);
        }
    }
}