using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace GZipTest.Core
{
    public static class FileUtils
    {
        public static IEnumerable<IReadOnlyList<byte>> ReadBytes(string filepath, int bufferSize, CancellationToken cancellationToken = default)
        {
            using var fileStream = File.OpenRead(filepath);

            var buffer = new byte[bufferSize];

            int bytesRead;
            while (!cancellationToken.IsCancellationRequested && (bytesRead = fileStream.Read(buffer)) != 0)
            {
                yield return bytesRead != buffer.Length
                    ? new ArraySegment<byte>(buffer, 0, bytesRead)
                    : buffer.ToArray();
            }
        }
    }
}
