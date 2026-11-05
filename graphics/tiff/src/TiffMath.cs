using System;

namespace tiff;

internal class TiffMath
{
    internal static ulong AddOffsets(ulong offset, ulong byteCount)
    {
        return offset + byteCount;
    }

    internal static long AddByteRangeEnd(long offset, long byteCount)
    {
        RequireNonNegative(offset, "offset");
        RequireNonNegative(byteCount, "byteCount");
        return offset + byteCount;
    }

    internal static bool IsByteRangeWithinLength(long offset, long byteCount, long length)
    {
        if (offset < 0 || byteCount < 0 || length < 0)
        {
            return false;
        }

        if (offset > length)
        {
            return false;
        }

        return byteCount <= length - offset;
    }

    internal static ulong AlignOffset(ulong offset, ulong alignment)
    {
        if (alignment == 0)
        {
            throw new ArgumentOutOfRangeException("alignment");
        }

        ulong remainder = offset % alignment;
        if (remainder == 0)
        {
            return offset;
        }

        return AddOffsets(offset, alignment - remainder);
    }

    internal static bool IsClassicOffset(ulong offset)
    {
        return offset <= uint.MaxValue;
    }

    internal static uint RequireClassicOffset(ulong offset)
    {
        if (!IsClassicOffset(offset))
        {
            throw new OverflowException("Offset does not fit in a classic TIFF 32-bit offset field.");
        }

        return (uint)offset;
    }

    internal static long MultiplyCounts(long count, long elementSize)
    {
        RequireNonNegative(count, "count");
        RequireNonNegative(elementSize, "elementSize");
        return count * elementSize;
    }

    internal static long DivideRoundUp(long value, long divisor)
    {
        RequireNonNegative(value, "value");
        if (divisor <= 0)
        {
            throw new ArgumentOutOfRangeException("divisor");
        }

        long result = value / divisor;
        if (value % divisor != 0)
        {
            result = result + 1;
        }

        return result;
    }

    internal static long CalculateByteCountFromBits(long bitCount)
    {
        return DivideRoundUp(bitCount, 8);
    }

    internal static long CalculateValueByteCount(long valueCount, int elementSize)
    {
        RequireNonNegative(valueCount, "valueCount");
        RequirePositive(elementSize, "elementSize");
        return MultiplyCounts(valueCount, elementSize);
    }

    internal static long CalculateScanlineByteCount(int width, int samplesPerPixel, int bitsPerSample)
    {
        RequirePositive(width, "width");
        RequirePositive(samplesPerPixel, "samplesPerPixel");
        RequirePositive(bitsPerSample, "bitsPerSample");
        long bitCount = (long)width * (long)samplesPerPixel * (long)bitsPerSample;
        return CalculateByteCountFromBits(bitCount);
    }

    internal static long CalculateRasterByteCount(int width, int length, int samplesPerPixel, int bitsPerSample)
    {
        RequirePositive(length, "length");
        long scanlineByteCount = CalculateScanlineByteCount(width, samplesPerPixel, bitsPerSample);
        return MultiplyCounts(scanlineByteCount, length);
    }

    internal static int CalculateStripCount(int imageLength, int rowsPerStrip)
    {
        RequirePositive(imageLength, "imageLength");
        RequirePositive(rowsPerStrip, "rowsPerStrip");
        long count = DivideRoundUp(imageLength, rowsPerStrip);
        if (count > int.MaxValue)
        {
            throw new OverflowException("Strip count does not fit in an Int32.");
        }

        return (int)count;
    }

    internal static int CalculateRowsInStrip(int imageLength, int rowsPerStrip, int stripIndex)
    {
        int stripCount = CalculateStripCount(imageLength, rowsPerStrip);
        if (stripIndex < 0 || stripIndex >= stripCount)
        {
            throw new ArgumentOutOfRangeException("stripIndex");
        }

        int firstRow = stripIndex * rowsPerStrip;
        int remainingRows = imageLength - firstRow;
        if (remainingRows < rowsPerStrip)
        {
            return remainingRows;
        }

        return rowsPerStrip;
    }

    internal static long CalculateStripByteCount(int width, int imageLength, int samplesPerPixel, int bitsPerSample, int rowsPerStrip, int stripIndex)
    {
        int rows = CalculateRowsInStrip(imageLength, rowsPerStrip, stripIndex);
        long scanlineByteCount = CalculateScanlineByteCount(width, samplesPerPixel, bitsPerSample);
        return MultiplyCounts(scanlineByteCount, rows);
    }

    internal static int CalculateTileCountAcross(int imageWidth, int tileWidth)
    {
        RequirePositive(imageWidth, "imageWidth");
        RequirePositive(tileWidth, "tileWidth");
        long count = DivideRoundUp(imageWidth, tileWidth);
        if (count > int.MaxValue)
        {
            throw new OverflowException("Tile count does not fit in an Int32.");
        }

        return (int)count;
    }

    internal static int CalculateTileCountDown(int imageLength, int tileLength)
    {
        RequirePositive(imageLength, "imageLength");
        RequirePositive(tileLength, "tileLength");
        long count = DivideRoundUp(imageLength, tileLength);
        if (count > int.MaxValue)
        {
            throw new OverflowException("Tile count does not fit in an Int32.");
        }

        return (int)count;
    }

    internal static int CalculateTileCount(int imageWidth, int imageLength, int tileWidth, int tileLength)
    {
        int across = CalculateTileCountAcross(imageWidth, tileWidth);
        int down = CalculateTileCountDown(imageLength, tileLength);
        return across * down;
    }

    internal static long CalculateTileStorageByteCount(int tileWidth, int tileLength, int samplesPerPixel, int bitsPerSample)
    {
        RequirePositive(tileLength, "tileLength");
        long scanlineByteCount = CalculateScanlineByteCount(tileWidth, samplesPerPixel, bitsPerSample);
        return MultiplyCounts(scanlineByteCount, tileLength);
    }

    internal static long CalculateTileImageByteCount(int imageWidth, int imageLength, int tileWidth, int tileLength, int samplesPerPixel, int bitsPerSample, int tileIndex)
    {
        int across = CalculateTileCountAcross(imageWidth, tileWidth);
        int tileCount = CalculateTileCount(imageWidth, imageLength, tileWidth, tileLength);
        if (tileIndex < 0 || tileIndex >= tileCount)
        {
            throw new ArgumentOutOfRangeException("tileIndex");
        }

        int tileColumn = tileIndex % across;
        int tileRow = tileIndex / across;
        int firstColumn = tileColumn * tileWidth;
        int firstRow = tileRow * tileLength;
        int actualWidth = imageWidth - firstColumn;
        int actualLength = imageLength - firstRow;
        if (actualWidth > tileWidth)
        {
            actualWidth = tileWidth;
        }

        if (actualLength > tileLength)
        {
            actualLength = tileLength;
        }

        return CalculateRasterByteCount(actualWidth, actualLength, samplesPerPixel, bitsPerSample);
    }

    internal static void RequireNonNegative(long value, string name)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    internal static void RequirePositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}
