namespace ttf;

internal static class CheckedFontMath
{
    internal static uint ReinterpretInt32AsUInt32(int value)
    {
        if (value >= 0)
        {
            return (uint)value;
        }

        return 0x80000000U + (uint)(value + int.MaxValue + 1);
    }

    internal static int ReinterpretUInt32AsInt32(uint value)
    {
        if (value <= int.MaxValue)
        {
            return (int)value;
        }

        return int.MinValue + (int)(value - 0x80000000U);
    }

    internal static int AddOffset(int baseOffset, int relativeOffset)
    {
        if (baseOffset < 0 || relativeOffset < 0)
        {
            throw FontFormatException.Create("Font offsets cannot be negative.");
        }

        if (relativeOffset > int.MaxValue - baseOffset)
        {
            throw FontFormatException.Create("Font offset addition overflowed.");
        }

        return baseOffset + relativeOffset;
    }

    internal static long AddOffset(long baseOffset, long relativeOffset)
    {
        if (baseOffset < 0 || relativeOffset < 0)
        {
            throw FontFormatException.Create("Font offsets cannot be negative.");
        }

        if (relativeOffset > long.MaxValue - baseOffset)
        {
            throw FontFormatException.Create("Font offset addition overflowed.");
        }

        return baseOffset + relativeOffset;
    }

    internal static int AddCounts(int firstCount, int secondCount)
    {
        if (firstCount < 0 || secondCount < 0)
        {
            throw FontFormatException.Create("Font counts cannot be negative.");
        }

        if (secondCount > int.MaxValue - firstCount)
        {
            throw FontFormatException.Create("Font count addition overflowed.");
        }

        return firstCount + secondCount;
    }

    internal static int MultiplyCount(int count, int itemSize)
    {
        if (count < 0 || itemSize < 0)
        {
            throw FontFormatException.Create("Font counts and item sizes cannot be negative.");
        }

        if (count != 0 && itemSize > int.MaxValue / count)
        {
            throw FontFormatException.Create("Font count multiplication overflowed.");
        }

        return count * itemSize;
    }

    internal static int GetRangeEnd(int offset, int count, int itemSize)
    {
        int byteLength = MultiplyCount(count, itemSize);
        return AddOffset(offset, byteLength);
    }

    internal static int Align(int value, int alignment)
    {
        if (value < 0)
        {
            throw FontFormatException.Create("A font size or offset cannot be negative before alignment.");
        }

        if (alignment <= 0)
        {
            throw new System.ArgumentOutOfRangeException("alignment");
        }

        int remainder = value % alignment;

        if (remainder == 0)
        {
            return value;
        }

        int padding = alignment - remainder;

        if (padding > int.MaxValue - value)
        {
            throw FontFormatException.Create("Font alignment overflowed.");
        }

        return value + padding;
    }

    internal static int AlignFour(int value)
    {
        return Align(value, 4);
    }

    internal static int GetPadding(int value, int alignment)
    {
        return Align(value, alignment) - value;
    }

    internal static double ScaleDesignUnits(int designUnits, int unitsPerEm, double pointSize)
    {
        if (unitsPerEm <= 0)
        {
            throw FontFormatException.Create("Units per em must be positive.");
        }

        if (double.IsNaN(pointSize) || double.IsInfinity(pointSize) || pointSize < 0)
        {
            throw new System.ArgumentOutOfRangeException("pointSize");
        }

        double result = (double)designUnits * pointSize / unitsPerEm;

        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            throw new System.ArgumentOutOfRangeException("pointSize");
        }

        return result;
    }

    internal static int RoundPdfUnits(int designUnits, int unitsPerEm)
    {
        if (unitsPerEm <= 0)
        {
            throw FontFormatException.Create("Units per em must be positive.");
        }

        long numerator = (long)designUnits * 1000L;
        bool negative = numerator < 0;
        long magnitude = negative ? -numerator : numerator;
        long rounded = magnitude / unitsPerEm;
        long remainder = magnitude % unitsPerEm;

        if (remainder * 2L >= unitsPerEm)
        {
            rounded += 1;
        }

        long result = negative ? -rounded : rounded;

        if (result < int.MinValue || result > int.MaxValue)
        {
            throw FontFormatException.Create("The PDF-unit coordinate is outside the supported integer range.");
        }

        return (int)result;
    }
}
