using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace image;

internal class ZlibHelper
{
    internal const int BufferLength = 4096;

    internal static byte[] Decompress(byte[]? bytes, long maxDecompressedBytes, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        if (!ValidateInput(bytes, maxDecompressedBytes, diagnostics, blockType))
        {
            return new byte[0];
        }

        if (bytes == null)
        {
            return new byte[0];
        }

        if (!HasSupportedHeader(bytes, diagnostics, blockType))
        {
            return new byte[0];
        }

        using (MemoryStream input = new MemoryStream(bytes))
        {
            using (ZLibStream zlib = new ZLibStream(input, CompressionMode.Decompress))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    byte[] buffer = new byte[BufferLength];

                    while (true)
                    {
                        long remainingLimit = maxDecompressedBytes - output.Length;

                        if (remainingLimit == 0)
                        {
                            int extra = zlib.Read(buffer, 0, 1);
                            if (extra > 0)
                            {
                                MetadataDiagnostic.AddLimitExceeded(diagnostics, "zlib.limit", "Decompressed byte limit was reached.", blockType, output.Length, maxDecompressedBytes);
                            }

                            return output.ToArray();
                        }

                        int request = BufferLength;
                        if (remainingLimit < request)
                        {
                            request = (int)remainingLimit;
                        }

                        int read = zlib.Read(buffer, 0, request);
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

    internal static bool ValidateInput(byte[]? bytes, long maxDecompressedBytes, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        if (bytes == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "zlib.bytes.null", "Compressed byte array was null.", blockType, 0, 0);
            return false;
        }

        if (maxDecompressedBytes < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "zlib.limit.negative", "Decompressed byte limit was negative.", blockType, 0, maxDecompressedBytes);
            return false;
        }

        if (bytes.Length < 2)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "zlib.header.truncated", "Zlib header was truncated.", blockType, 0, bytes.Length);
            return false;
        }

        return true;
    }

    internal static bool HasSupportedHeader(byte[] bytes, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        int cmf = bytes[0];
        int flg = bytes[1];
        int compressionMethod = cmf & 15;
        int compressionInfo = (cmf >> 4) & 15;
        int headerCheck = (cmf * 256) + flg;

        if (compressionMethod != 8)
        {
            MetadataDiagnostic.AddUnsupported(diagnostics, "zlib.method.unsupported", "Zlib compression method was unsupported.", blockType);
            return false;
        }

        if (compressionInfo > 7)
        {
            MetadataDiagnostic.AddUnsupported(diagnostics, "zlib.window.unsupported", "Zlib compression window size was unsupported.", blockType);
            return false;
        }

        if ((headerCheck % 31) != 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "zlib.header.invalid", "Zlib header check bits were invalid.", blockType, 0, 2);
            return false;
        }

        if ((flg & 32) != 0)
        {
            MetadataDiagnostic.AddUnsupported(diagnostics, "zlib.dictionary.unsupported", "Zlib streams with preset dictionaries are unsupported.", blockType);
            return false;
        }

        return true;
    }
}
