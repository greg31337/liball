using System;
using System.IO;
using System.IO.Compression;

namespace tiff;

internal class TiffDeflateCodec
{
    internal static TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        byte[] bytes = DecodeBytes(context, source, diagnostics);
        return TiffCodecResult.Create(bytes, diagnostics);
    }

    internal static TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        byte[] bytes = EncodeBytes(context, source, diagnostics);
        return TiffCodecResult.Create(bytes, diagnostics);
    }

    internal static byte[] DecodeBytes(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (!TiffCodec.ValidateDecodeContext(context, source, diagnostics))
        {
            return new byte[0];
        }

        byte[] destination = new byte[(int)context.ExpectedDecodedByteCount];
        byte[] compressedBytes = TiffIfdEntry.CopyBytes(source);
        MemoryStream sourceStream = new MemoryStream(compressedBytes);
        Stream deflateStream = CreateDecompressionStream(sourceStream, source);
        ReadDecodedBytes(deflateStream, destination, diagnostics);
        deflateStream.Dispose();
        sourceStream.Dispose();

        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        return destination;
    }

    internal static byte[] EncodeBytes(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (!TiffCodec.ValidateEncodeContext(context, diagnostics))
        {
            return new byte[0];
        }

        if (context.ExpectedDecodedByteCount != (ulong)source.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Deflate decoded byte count does not match the expected byte count.");
            return new byte[0];
        }

        int maximumEncodedByteCount = CalculateMaximumEncodedByteCount(source.Length, context.Limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] decodedBytes = TiffIfdEntry.CopyBytes(source);
        MemoryStream destinationStream = new MemoryStream(maximumEncodedByteCount);
        Stream deflateStream = CreateCompressionStream(destinationStream);
        deflateStream.Write(decodedBytes, 0, decodedBytes.Length);
        deflateStream.Dispose();
        byte[] encodedBytes = destinationStream.ToArray();
        destinationStream.Dispose();
        return encodedBytes;
    }

    internal static int CalculateMaximumEncodedByteCount(int sourceLength, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        long maximumEncodedByteCount = (long)sourceLength + (long)sourceLength / 4096 + (long)sourceLength / 16384 + (long)sourceLength / 33554432 + 13;
        if (maximumEncodedByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Deflate encoded byte count is too large to allocate.");
            return 0;
        }

        TiffLimits normalizedLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!normalizedLimits.AllowsAllocation(maximumEncodedByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Deflate encoded byte count exceeds the configured allocation limit.");
            return 0;
        }

        return (int)maximumEncodedByteCount;
    }

    internal static Stream CreateDecompressionStream(MemoryStream sourceStream, ReadOnlySpan<byte> source)
    {
        if (IsZLibWrapped(source))
        {
            return new ZLibStream(sourceStream, CompressionMode.Decompress);
        }

        return new DeflateStream(sourceStream, CompressionMode.Decompress);
    }

    internal static Stream CreateCompressionStream(MemoryStream destinationStream)
    {
        return new ZLibStream(destinationStream, CompressionLevel.SmallestSize);
    }

    internal static void ReadDecodedBytes(Stream deflateStream, byte[] destination, TiffDiagnostics diagnostics)
    {
        int destinationIndex = 0;
        while (destinationIndex < destination.Length)
        {
            int readCount = deflateStream.Read(destination, destinationIndex, destination.Length - destinationIndex);
            if (readCount == 0)
            {
                diagnostics.AddError(TiffErrorCode.TruncatedStream, "Deflate stream ended before the expected decoded byte count was reached.");
                return;
            }

            destinationIndex = destinationIndex + readCount;
        }

        if (deflateStream.ReadByte() >= 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Deflate stream contains data after the expected decoded byte count was reached.");
        }
    }

    internal static bool IsZLibWrapped(ReadOnlySpan<byte> source)
    {
        if (source.Length < 2)
        {
            return false;
        }

        int compressionMethodAndInfo = source[0];
        int flags = source[1];
        if ((compressionMethodAndInfo & 15) != 8)
        {
            return false;
        }

        int headerValue = compressionMethodAndInfo * 256 + flags;
        if (headerValue % 31 != 0)
        {
            return false;
        }

        return true;
    }
}
