namespace avif;

internal static class RangeConverter
{
    internal static int ConvertSample(
        int value,
        int sourceBitDepth,
        AvifRange sourceRange,
        int destinationBitDepth,
        AvifRange destinationRange,
        bool chroma)
    {
        int sourceMinimum = GetMinimum(sourceBitDepth, sourceRange);
        int sourceMaximum = GetMaximum(
            sourceBitDepth, sourceRange, chroma);
        int destinationMinimum = GetMinimum(
            destinationBitDepth, destinationRange);
        int destinationMaximum = GetMaximum(
            destinationBitDepth, destinationRange, chroma);
        if (value <= sourceMinimum)
        {
            return destinationMinimum;
        }
        if (value >= sourceMaximum)
        {
            return destinationMaximum;
        }

        int sourceSpan = sourceMaximum - sourceMinimum;
        int destinationSpan = destinationMaximum - destinationMinimum;
        long numerator = (long)(value - sourceMinimum)
            * destinationSpan;
        return destinationMinimum
            + (int)((numerator + sourceSpan / 2) / sourceSpan);
    }

    internal static int GetMinimum(int bitDepth, AvifRange range)
    {
        return range == AvifRange.Full ? 0 : 16 << (bitDepth - 8);
    }

    internal static int GetMaximum(
        int bitDepth,
        AvifRange range,
        bool chroma)
    {
        if (range == AvifRange.Full)
        {
            return (1 << bitDepth) - 1;
        }
        int endpoint = chroma ? 240 : 235;
        return endpoint << (bitDepth - 8);
    }

    internal static bool IsValidBitDepth(int value)
    {
        return value == 8 || value == 10 || value == 12 || value == 16;
    }

    internal static bool IsValidRange(AvifRange value)
    {
        return value == AvifRange.Limited || value == AvifRange.Full;
    }
}
