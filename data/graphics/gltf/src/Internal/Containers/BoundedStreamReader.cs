using System;
using System.IO;

namespace gltf;

internal class BoundedStreamReader
{
    internal const int BufferByteLength = 81920;

    internal byte[] ReadAllBytes(Stream stream, int maximumByteLength)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (maximumByteLength < 0)
        {
            throw new ArgumentOutOfRangeException("maximumByteLength");
        }

        if (!stream.CanRead)
        {
            throw new ArgumentException("The input stream must be readable.", "stream");
        }

        byte[] readBuffer = new byte[BufferByteLength];
        using (MemoryStream bufferedBytes = new MemoryStream())
        {
            int totalByteLength = 0;
            while (true)
            {
                int remainingByteLength = maximumByteLength - totalByteLength;
                int requestedByteLength = readBuffer.Length;
                if (requestedByteLength > remainingByteLength)
                {
                    requestedByteLength = remainingByteLength + 1;
                }

                int readByteLength = stream.Read(readBuffer, 0, requestedByteLength);
                if (readByteLength == 0)
                {
                    break;
                }

                if (readByteLength > remainingByteLength)
                {
                    throw new GltfParseException(
                        $"Input stream exceeds the configured maximum {maximumByteLength} at byte {maximumByteLength}.");
                }

                bufferedBytes.Write(readBuffer, 0, readByteLength);
                totalByteLength += readByteLength;
            }

            return bufferedBytes.ToArray();
        }
    }
}
