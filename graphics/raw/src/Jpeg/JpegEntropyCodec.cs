namespace raw;

internal static class JpegEntropyCodec
{
    internal static byte[] Unstuff(System.ReadOnlyMemory<byte> source, JpegEntropySegment? segment, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        if (segment == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A JPEG entropy segment is required.", -1);
        }
        CheckedMath.FileSize(source.Length, limits);
        CheckedMath.RangeEnd(segment.Offset, segment.EncodedLength, source.Length);
        if (segment.DecodedLength < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The JPEG entropy decoded length cannot be negative.", segment.Offset);
        }
        CheckedMath.AllocationSize(segment.DecodedLength, 1, limits);
        cancellationToken.ThrowIfCancellationRequested();

        byte[] destination = new byte[segment.DecodedLength];
        System.ReadOnlySpan<byte> encoded = source.Span.Slice(segment.Offset, segment.EncodedLength);
        int inputOffset = 0;
        int outputOffset = 0;
        while (inputOffset < encoded.Length)
        {
            if ((inputOffset & 65535) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            byte value = encoded[inputOffset];
            inputOffset++;
            if (value == 0xff)
            {
                if (inputOffset >= encoded.Length || encoded[inputOffset] != 0x00)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG entropy segment contains an unescaped marker prefix.", segment.Offset + inputOffset - 1);
                }
                inputOffset++;
            }
            if (outputOffset >= destination.Length)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "JPEG entropy data exceeds its parsed unstuffed length.", segment.Offset + inputOffset - 1);
            }
            destination[outputOffset] = value;
            outputOffset++;
        }

        if (outputOffset != destination.Length)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "JPEG entropy data ends before its parsed unstuffed length.", segment.Offset + segment.EncodedLength);
        }
        return destination;
    }
}
