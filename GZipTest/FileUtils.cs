using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace GZipTest
{
    public static class FileUtils
    {
        public static IEnumerable<IReadOnlyList<byte>> ReadBytes(string filepath, int bufferSize)
        {
            using var fileStream = File.OpenRead(filepath);

            var buffer = new byte[bufferSize];

            int bytesRead;
            while ((bytesRead = fileStream.Read(buffer)) != 0)
            {
                yield return bytesRead != buffer.Length
                    ? new ArraySegment<byte>(buffer, 0, bytesRead)
                    : buffer.ToArray();
            }
        }


        public static IEnumerable<byte[]> ReadBytes_original(string filepath, int bufferSize)
        {
            using var fileStream = File.OpenRead(filepath);

            var buffer = new byte[bufferSize];
            int readBytes;
            while ((readBytes = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                if (readBytes != buffer.Length)
                {
                    var lastChunck = new byte[readBytes];
                    Array.Copy(buffer, lastChunck, readBytes);
                    yield return lastChunck;
                }

                yield return buffer.ToArray();
            }

        }
    }
}
