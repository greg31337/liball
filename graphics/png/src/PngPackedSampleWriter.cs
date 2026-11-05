namespace png;

internal class PngPackedSampleWriter
{
    internal static byte[] Pack(byte[] samples, int offset, int sampleCount, PngBitDepth bitDepth)
    {
        ArgumentNullException.ThrowIfNull(samples);

        int bitsPerSample = GetPackedBitDepth(bitDepth);
        ValidateSampleRange(samples, offset, sampleCount);
        int requiredBytes = CalculateRequiredBytes(sampleCount, bitsPerSample);
        byte[] packed = new byte[requiredBytes];
        PackInto(packed, 0, samples, offset, sampleCount, bitDepth);
        return packed;
    }

    internal static byte[] PackRow(byte[] samples, PngScanlineLayout layout)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(layout);

        ValidatePackedLayout(layout);
        if (layout.RowDataBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        byte[] packed = Pack(samples, 0, layout.Width, layout.BitDepth);
        if (packed.Length != layout.RowDataBytes)
        {
            throw new OverflowException();
        }

        return packed;
    }

    internal static byte[] Pack1Bit(byte[] samples, int offset, int sampleCount)
    {
        return Pack(samples, offset, sampleCount, PngBitDepth.One);
    }

    internal static byte[] Pack2Bit(byte[] samples, int offset, int sampleCount)
    {
        return Pack(samples, offset, sampleCount, PngBitDepth.Two);
    }

    internal static byte[] Pack4Bit(byte[] samples, int offset, int sampleCount)
    {
        return Pack(samples, offset, sampleCount, PngBitDepth.Four);
    }

    internal static void PackInto(byte[] destination, int destinationOffset, byte[] samples, int sampleOffset, int sampleCount, PngBitDepth bitDepth)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(samples);

        int bitsPerSample = GetPackedBitDepth(bitDepth);
        ValidateSampleRange(samples, sampleOffset, sampleCount);
        int requiredBytes = CalculateRequiredBytes(sampleCount, bitsPerSample);
        if (destinationOffset < 0 || destinationOffset > destination.Length)
        {
            throw new ArgumentOutOfRangeException("destinationOffset");
        }

        if (requiredBytes > destination.Length - destinationOffset)
        {
            throw new ArgumentOutOfRangeException("destinationOffset");
        }

        ClearPackedBytes(destination, destinationOffset, requiredBytes);

        int sampleIndex = 0;
        while (sampleIndex < sampleCount)
        {
            byte sample = samples[sampleOffset + sampleIndex];
            ValidateSampleValue(sample, bitsPerSample);
            WriteSample(destination, destinationOffset, sampleIndex, bitsPerSample, sample);
            sampleIndex += 1;
        }
    }

    internal static void WriteSample(byte[] destination, int offset, int sampleIndex, int bitsPerSample, byte sample)
    {
        int bitIndex = sampleIndex * bitsPerSample;
        int byteIndex = offset + bitIndex / 8;
        int bitOffset = bitIndex % 8;
        int shift = 8 - bitsPerSample - bitOffset;
        destination[byteIndex] = (byte)(destination[byteIndex] | (sample << shift));
    }

    internal static void ClearPackedBytes(byte[] destination, int offset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[offset + index] = 0;
            index += 1;
        }
    }

    internal static int GetPackedBitDepth(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.One:
                return 1;
            case PngBitDepth.Two:
                return 2;
            case PngBitDepth.Four:
                return 4;
            default:
                throw new ArgumentOutOfRangeException("bitDepth");
        }
    }

    internal static void ValidateSampleRange(byte[] samples, int offset, int sampleCount)
    {
        if (offset < 0 || offset > samples.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (sampleCount < 0 || sampleCount > samples.Length - offset)
        {
            throw new ArgumentOutOfRangeException("sampleCount");
        }
    }

    internal static void ValidateSampleValue(byte sample, int bitsPerSample)
    {
        int maximumValue = (1 << bitsPerSample) - 1;
        if (sample > maximumValue)
        {
            throw new ArgumentOutOfRangeException("samples");
        }
    }

    internal static int CalculateRequiredBytes(int sampleCount, int bitsPerSample)
    {
        long totalBits = (long)sampleCount * bitsPerSample;
        long requiredBytes = (totalBits + 7) / 8;
        if (requiredBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        return (int)requiredBytes;
    }

    internal static void ValidatePackedLayout(PngScanlineLayout layout)
    {
        if (layout.ColorType != PngColorType.Grayscale && layout.ColorType != PngColorType.IndexedColor)
        {
            throw new ArgumentOutOfRangeException("ColorType");
        }

        GetPackedBitDepth(layout.BitDepth);
    }
}
