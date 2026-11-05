using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace image;

internal class GzipHelper
{
    internal const int BufferLength = 4096;
    internal const int MinimumLength = 18;

    internal static GzipDecompressionResult Decompress(byte[] bytes, long maxDecompressedBytes, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        GzipDecompressionResult result = new GzipDecompressionResult();
        if (!ValidateInput(bytes, maxDecompressedBytes, diagnostics, blockType))
        {
            return result;
        }

        if (maxDecompressedBytes == 0)
        {
            MetadataDiagnostic.AddLimitExceeded(diagnostics, "gzip.limit", "Gzip decompressed byte limit was reached.", blockType, 0, 0);
            return result;
        }

        using (MemoryStream input = new MemoryStream(bytes))
        {
            using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    byte[] buffer = new byte[BufferLength];
                    while (true)
                    {
                        long remaining = maxDecompressedBytes - output.Length;
                        if (remaining == 0)
                        {
                            int extra = gzip.Read(buffer, 0, 1);
                            if (extra > 0)
                            {
                                MetadataDiagnostic.AddLimitExceeded(diagnostics, "gzip.limit", "Gzip decompressed byte limit was reached.", blockType, output.Length, maxDecompressedBytes);
                                result.Data = output.ToArray();
                                return result;
                            }

                            result.Data = output.ToArray();
                            result.Complete = true;
                            return result;
                        }

                        int request = BufferLength;
                        if (remaining < request)
                        {
                            request = (int)remaining;
                        }

                        int read = gzip.Read(buffer, 0, request);
                        if (read == 0)
                        {
                            result.Data = output.ToArray();
                            result.Complete = true;
                            return result;
                        }

                        output.Write(buffer, 0, read);
                    }
                }
            }
        }
    }

    internal static bool ValidateInput(byte[] bytes, long maxDecompressedBytes, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        if (bytes == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "gzip.bytes.null", "Gzip byte array was null.", blockType, 0, 0);
            return false;
        }

        if (maxDecompressedBytes < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "gzip.limit.negative", "Gzip decompressed byte limit was negative.", blockType, 0, maxDecompressedBytes);
            return false;
        }

        if (bytes.Length < MinimumLength)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "gzip.truncated", "Gzip stream was too short to contain a header and trailer.", blockType, 0, bytes.Length);
            return false;
        }

        if (bytes[0] != 31 || bytes[1] != 139)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "gzip.signature.invalid", "Gzip signature was invalid.", blockType, 0, 2);
            return false;
        }

        if (bytes[2] != 8)
        {
            MetadataDiagnostic.AddUnsupported(diagnostics, "gzip.method.unsupported", "Gzip compression method was unsupported.", blockType);
            return false;
        }

        if ((bytes[3] & 224) != 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "gzip.flags.invalid", "Gzip reserved flags were set.", blockType, 3, 1);
            return false;
        }

        return ValidateOptionalHeader(bytes, diagnostics, blockType);
    }

    internal static bool ValidateOptionalHeader(byte[] bytes, List<MetadataDiagnostic> diagnostics, string blockType)
    {
        int flags = bytes[3];
        int offset = 10;
        int trailerOffset = bytes.Length - 8;
        if ((flags & 4) != 0)
        {
            if (offset + 2 > trailerOffset)
            {
                MetadataDiagnostic.AddTruncated(diagnostics, "gzip.extra.truncated", "Gzip extra-field length was truncated.", blockType, offset, 2);
                return false;
            }

            int extraLength = bytes[offset] | (bytes[offset + 1] << 8);
            offset = offset + 2 + extraLength;
            if (offset > trailerOffset)
            {
                MetadataDiagnostic.AddTruncated(diagnostics, "gzip.extra.truncated", "Gzip extra field exceeded the compressed stream.", blockType, offset - extraLength, extraLength);
                return false;
            }
        }

        if ((flags & 8) != 0)
        {
            offset = FindTerminator(bytes, offset, trailerOffset);
            if (offset < 0)
            {
                MetadataDiagnostic.AddTruncated(diagnostics, "gzip.name.truncated", "Gzip original file name was not terminated.", blockType, 10, trailerOffset - 10);
                return false;
            }
        }

        if ((flags & 16) != 0)
        {
            int start = offset;
            offset = FindTerminator(bytes, offset, trailerOffset);
            if (offset < 0)
            {
                MetadataDiagnostic.AddTruncated(diagnostics, "gzip.comment.truncated", "Gzip comment was not terminated.", blockType, start, trailerOffset - start);
                return false;
            }
        }

        if ((flags & 2) != 0)
        {
            offset = offset + 2;
            if (offset > trailerOffset)
            {
                MetadataDiagnostic.AddTruncated(diagnostics, "gzip.header-crc.truncated", "Gzip header CRC was truncated.", blockType, offset - 2, 2);
                return false;
            }
        }

        if (offset >= trailerOffset)
        {
            MetadataDiagnostic.AddTruncated(diagnostics, "gzip.payload.truncated", "Gzip compressed payload was missing.", blockType, offset, 1);
            return false;
        }

        return true;
    }

    internal static int FindTerminator(byte[] bytes, int offset, int limit)
    {
        while (offset < limit)
        {
            if (bytes[offset] == 0)
            {
                return offset + 1;
            }

            offset = offset + 1;
        }

        return -1;
    }
}
