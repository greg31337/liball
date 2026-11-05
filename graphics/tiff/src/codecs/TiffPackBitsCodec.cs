using System;

namespace tiff;

internal class TiffPackBitsCodec
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
        int sourceIndex = 0;
        int destinationIndex = 0;
        int expectedLength = destination.Length;
        while (sourceIndex < source.Length && destinationIndex < expectedLength)
        {
            int header = source[sourceIndex];
            sourceIndex = sourceIndex + 1;
            if (header <= 127)
            {
                int literalCount = header + 1;
                CopyLiteralRun(source, literalCount, destination, sourceIndex, destinationIndex, diagnostics);
                if (diagnostics.HasErrors())
                {
                    return new byte[0];
                }

                sourceIndex = sourceIndex + literalCount;
                destinationIndex = destinationIndex + literalCount;
            }
            else if (header == 128)
            {
            }
            else
            {
                int repeatCount = 257 - header;
                CopyRepeatedRun(source, repeatCount, destination, sourceIndex, destinationIndex, diagnostics);
                if (diagnostics.HasErrors())
                {
                    return new byte[0];
                }

                sourceIndex = sourceIndex + 1;
                destinationIndex = destinationIndex + repeatCount;
            }
        }

        if (destinationIndex != expectedLength)
        {
            diagnostics.AddError(TiffErrorCode.TruncatedStream, "PackBits stream ended before the expected decoded byte count was reached.");
            return new byte[0];
        }

        if (!RemainingBytesAreNoOps(source, sourceIndex))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "PackBits stream contains data after the expected decoded byte count was reached.");
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
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "PackBits decoded byte count does not match the expected byte count.");
            return new byte[0];
        }

        int maximumEncodedByteCount = CalculateMaximumEncodedByteCount(source.Length, context.Limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] destination = new byte[maximumEncodedByteCount];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (sourceIndex < source.Length)
        {
            int repeatedByteCount = CountRepeatedBytes(source, sourceIndex, 128);
            if (repeatedByteCount >= 3)
            {
                destinationIndex = WriteRepeatedRun(source[sourceIndex], repeatedByteCount, destination, destinationIndex);
                sourceIndex = sourceIndex + repeatedByteCount;
            }
            else
            {
                int literalByteCount = CountLiteralBytes(source, sourceIndex);
                destinationIndex = WriteLiteralRun(source, sourceIndex, literalByteCount, destination, destinationIndex);
                sourceIndex = sourceIndex + literalByteCount;
            }
        }

        return TrimEncodedBytes(destination, destinationIndex);
    }

    internal static int CalculateMaximumEncodedByteCount(int sourceLength, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (sourceLength == 0)
        {
            return 0;
        }

        long literalRunCount = ((long)sourceLength + 127) / 128;
        long maximumEncodedByteCount = (long)sourceLength + literalRunCount;
        if (maximumEncodedByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "PackBits encoded byte count is too large to allocate.");
            return 0;
        }

        TiffLimits normalizedLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!normalizedLimits.AllowsAllocation(maximumEncodedByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "PackBits encoded byte count exceeds the configured allocation limit.");
            return 0;
        }

        return (int)maximumEncodedByteCount;
    }

    internal static int CountRepeatedBytes(ReadOnlySpan<byte> source, int sourceIndex, int maximumCount)
    {
        byte value = source[sourceIndex];
        int count = 1;
        while (sourceIndex + count < source.Length && count < maximumCount)
        {
            if (source[sourceIndex + count] != value)
            {
                return count;
            }

            count = count + 1;
        }

        return count;
    }

    internal static int CountLiteralBytes(ReadOnlySpan<byte> source, int sourceIndex)
    {
        int count = 0;
        while (sourceIndex + count < source.Length && count < 128)
        {
            int repeatedByteCount = CountRepeatedBytes(source, sourceIndex + count, 128);
            if (repeatedByteCount >= 3)
            {
                if (count == 0)
                {
                    return repeatedByteCount;
                }

                return count;
            }

            count = count + 1;
        }

        return count;
    }

    internal static int WriteLiteralRun(ReadOnlySpan<byte> source, int sourceIndex, int count, byte[] destination, int destinationIndex)
    {
        destination[destinationIndex] = (byte)(count - 1);
        int index = 0;
        while (index < count)
        {
            destination[destinationIndex + 1 + index] = source[sourceIndex + index];
            index = index + 1;
        }

        return destinationIndex + count + 1;
    }

    internal static int WriteRepeatedRun(byte value, int count, byte[] destination, int destinationIndex)
    {
        destination[destinationIndex] = (byte)(257 - count);
        destination[destinationIndex + 1] = value;
        return destinationIndex + 2;
    }

    internal static byte[] TrimEncodedBytes(byte[] source, int length)
    {
        byte[] trimmed = new byte[length];
        int index = 0;
        while (index < length)
        {
            trimmed[index] = source[index];
            index = index + 1;
        }

        return trimmed;
    }

    internal static void CopyLiteralRun(ReadOnlySpan<byte> source, int literalCount, byte[] destination, int sourceIndex, int destinationIndex, TiffDiagnostics diagnostics)
    {
        if (sourceIndex > source.Length - literalCount)
        {
            diagnostics.AddError(TiffErrorCode.TruncatedStream, "PackBits literal run extends past the compressed input.");
            return;
        }

        if (destinationIndex > destination.Length - literalCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "PackBits literal run exceeds the expected decoded byte count.");
            return;
        }

        int index = 0;
        while (index < literalCount)
        {
            destination[destinationIndex + index] = source[sourceIndex + index];
            index = index + 1;
        }
    }

    internal static void CopyRepeatedRun(ReadOnlySpan<byte> source, int repeatCount, byte[] destination, int sourceIndex, int destinationIndex, TiffDiagnostics diagnostics)
    {
        if (sourceIndex >= source.Length)
        {
            diagnostics.AddError(TiffErrorCode.TruncatedStream, "PackBits repeated run is missing the repeated byte.");
            return;
        }

        if (destinationIndex > destination.Length - repeatCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "PackBits repeated run exceeds the expected decoded byte count.");
            return;
        }

        byte value = source[sourceIndex];
        int index = 0;
        while (index < repeatCount)
        {
            destination[destinationIndex + index] = value;
            index = index + 1;
        }
    }

    internal static bool RemainingBytesAreNoOps(ReadOnlySpan<byte> source, int sourceIndex)
    {
        int index = sourceIndex;
        while (index < source.Length)
        {
            if (source[index] != 128)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
