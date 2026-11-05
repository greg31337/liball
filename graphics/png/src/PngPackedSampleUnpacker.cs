namespace png;

internal class PngPackedSampleUnpacker
{
    internal static byte[] Unpack(byte[] data, int offset, int sampleCount, PngBitDepth bitDepth)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (sampleCount < 0)
        {
            throw new ArgumentOutOfRangeException("sampleCount");
        }

        int bitsPerSample = GetPackedBitDepth(bitDepth);
        int requiredBytes = CalculateRequiredBytes(sampleCount, bitsPerSample);
        if (requiredBytes > data.Length - offset)
        {
            throw new ArgumentOutOfRangeException("sampleCount");
        }

        byte[] samples = new byte[sampleCount];
        int sampleIndex = 0;
        while (sampleIndex < sampleCount)
        {
            samples[sampleIndex] = ReadSample(data, offset, sampleIndex, bitsPerSample);
            sampleIndex += 1;
        }

        return samples;
    }

    internal static byte[] UnpackRow(byte[] rowData, PngScanlineLayout layout)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(layout);

        ValidatePackedLayout(layout);
        if (layout.RowDataBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        if (rowData.Length < layout.RowDataBytes)
        {
            throw new ArgumentOutOfRangeException("rowData");
        }

        return Unpack(rowData, 0, layout.Width, layout.BitDepth);
    }

    internal static byte[] Unpack1Bit(byte[] data, int offset, int sampleCount)
    {
        return Unpack(data, offset, sampleCount, PngBitDepth.One);
    }

    internal static byte[] Unpack2Bit(byte[] data, int offset, int sampleCount)
    {
        return Unpack(data, offset, sampleCount, PngBitDepth.Two);
    }

    internal static byte[] Unpack4Bit(byte[] data, int offset, int sampleCount)
    {
        return Unpack(data, offset, sampleCount, PngBitDepth.Four);
    }

    internal static byte ReadSample(byte[] data, int offset, int sampleIndex, int bitsPerSample)
    {
        int bitIndex = sampleIndex * bitsPerSample;
        int byteIndex = offset + bitIndex / 8;
        int bitOffset = bitIndex % 8;
        int shift = 8 - bitsPerSample - bitOffset;
        int mask = (1 << bitsPerSample) - 1;
        return (byte)((data[byteIndex] >> shift) & mask);
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
