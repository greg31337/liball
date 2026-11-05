namespace png;

internal class PngSample16BitRow
{
    internal static ushort[] Read(byte[] rowData, PngScanlineLayout layout)
    {
        return Read(rowData, 0, layout);
    }

    internal static ushort[] Read(byte[] rowData, int offset, PngScanlineLayout layout)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(layout);

        ValidateLayout(layout);
        int sampleCount = CalculateSampleCount(layout);
        int byteCount = CalculateByteCount(sampleCount);
        ValidateByteArrayRange(rowData, offset, byteCount, "offset");

        ushort[] samples = new ushort[sampleCount];
        int sampleIndex = 0;
        int byteOffset = offset;
        while (sampleIndex < sampleCount)
        {
            samples[sampleIndex] = PngBinaryReader.ReadUInt16(rowData, byteOffset);
            byteOffset += 2;
            sampleIndex += 1;
        }

        return samples;
    }

    internal static byte[] Write(ushort[] samples, PngScanlineLayout layout)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(layout);

        ValidateLayout(layout);
        int sampleCount = CalculateSampleCount(layout);
        ValidateSampleArrayRange(samples, 0, sampleCount, "samples");
        byte[] rowData = new byte[CalculateByteCount(sampleCount)];
        WriteInto(rowData, 0, samples, 0, layout);
        return rowData;
    }

    internal static void WriteInto(byte[] destination, int destinationOffset, ushort[] samples, int sampleOffset, PngScanlineLayout layout)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(layout);

        ValidateLayout(layout);
        int sampleCount = CalculateSampleCount(layout);
        int byteCount = CalculateByteCount(sampleCount);
        ValidateByteArrayRange(destination, destinationOffset, byteCount, "destinationOffset");
        ValidateSampleArrayRange(samples, sampleOffset, sampleCount, "sampleOffset");

        int sampleIndex = 0;
        int byteOffset = destinationOffset;
        while (sampleIndex < sampleCount)
        {
            PngBinaryWriter.WriteUInt16(destination, byteOffset, samples[sampleOffset + sampleIndex]);
            byteOffset += 2;
            sampleIndex += 1;
        }
    }

    internal static void ValidateLayout(PngScanlineLayout layout)
    {
        if (layout.BitDepth != PngBitDepth.Sixteen)
        {
            throw new ArgumentOutOfRangeException("BitDepth");
        }

        if (layout.RowDataBytes > int.MaxValue)
        {
            throw new OverflowException();
        }
    }

    internal static int CalculateSampleCount(PngScanlineLayout layout)
    {
        long sampleCount = (long)layout.Width * layout.SamplesPerPixel;
        if (sampleCount > int.MaxValue)
        {
            throw new OverflowException();
        }

        long byteCount = sampleCount * 2;
        if (byteCount != layout.RowDataBytes)
        {
            throw new ArgumentOutOfRangeException("layout");
        }

        return (int)sampleCount;
    }

    internal static int CalculateByteCount(int sampleCount)
    {
        if (sampleCount > int.MaxValue / 2)
        {
            throw new OverflowException();
        }

        return sampleCount * 2;
    }

    internal static void ValidateByteArrayRange(byte[] data, int offset, int length, string name)
    {
        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException(name);
        }

        if (length > data.Length - offset)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    internal static void ValidateSampleArrayRange(ushort[] data, int offset, int length, string name)
    {
        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException(name);
        }

        if (length > data.Length - offset)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}
