namespace png;

internal class PngSample8BitRow
{
    internal static byte[] Read(byte[] rowData, PngScanlineLayout layout)
    {
        return Read(rowData, 0, layout);
    }

    internal static byte[] Read(byte[] rowData, int offset, PngScanlineLayout layout)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(layout);

        ValidateLayout(layout);
        int sampleCount = CalculateSampleCount(layout);
        ValidateArrayRange(rowData, offset, sampleCount, "offset");

        byte[] samples = new byte[sampleCount];
        Copy(rowData, offset, samples, 0, sampleCount);
        return samples;
    }

    internal static byte[] Write(byte[] samples, PngScanlineLayout layout)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(layout);

        ValidateLayout(layout);
        int sampleCount = CalculateSampleCount(layout);
        ValidateArrayRange(samples, 0, sampleCount, "samples");
        byte[] rowData = new byte[sampleCount];
        Copy(samples, 0, rowData, 0, sampleCount);
        return rowData;
    }

    internal static void WriteInto(byte[] destination, int destinationOffset, byte[] samples, int sampleOffset, PngScanlineLayout layout)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(layout);

        ValidateLayout(layout);
        int sampleCount = CalculateSampleCount(layout);
        ValidateArrayRange(destination, destinationOffset, sampleCount, "destinationOffset");
        ValidateArrayRange(samples, sampleOffset, sampleCount, "sampleOffset");
        Copy(samples, sampleOffset, destination, destinationOffset, sampleCount);
    }

    internal static void ValidateLayout(PngScanlineLayout layout)
    {
        if (layout.BitDepth != PngBitDepth.Eight)
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

        if (sampleCount != layout.RowDataBytes)
        {
            throw new ArgumentOutOfRangeException("layout");
        }

        return (int)sampleCount;
    }

    internal static void ValidateArrayRange(byte[] data, int offset, int length, string name)
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

    internal static void Copy(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}
