using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace image;

internal class BrotliHelper
{
    internal const int BufferLength = 4096;

    internal static byte[] Decompress(byte[] bytes, long maxDecompressedBytes, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        if (bytes == null || bytes.Length == 0)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "brotli.payload.truncated", "Brotli payload was empty.", blockType, 0, 1);
            return new byte[0];
        }

        if (maxDecompressedBytes <= 0)
        {
            MetadataDiagnostic.AddLimitExceeded(diagnostics, "brotli.limit", "Brotli decompressed byte limit was reached.", blockType, 0, maxDecompressedBytes);
            return new byte[0];
        }

        using (MemoryStream input = new MemoryStream(bytes))
        {
            using (BrotliStream brotli = new BrotliStream(input, CompressionMode.Decompress))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    byte[] buffer = new byte[BufferLength];
                    while (true)
                    {
                        long remaining = maxDecompressedBytes - output.Length;
                        if (remaining == 0)
                        {
                            int extra = brotli.Read(buffer, 0, 1);
                            if (extra > 0)
                            {
                                MetadataDiagnostic.AddLimitExceeded(diagnostics, "brotli.limit", "Brotli decompressed byte limit was reached.", blockType, output.Length, maxDecompressedBytes);
                            }

                            return output.ToArray();
                        }

                        int request = BufferLength;
                        if (remaining < request)
                        {
                            request = (int)remaining;
                        }

                        int read = brotli.Read(buffer, 0, request);
                        if (read == 0)
                        {
                            return output.ToArray();
                        }

                        output.Write(buffer, 0, read);
                    }
                }
            }
        }
    }
}
