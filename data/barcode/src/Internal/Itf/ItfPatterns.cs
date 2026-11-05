namespace barcode;

internal static class ItfPatterns
{
    internal const int DigitCount = 10;
    internal const int DigitElementCount = 5;
    internal const int WideElementCount = 2;
    internal const int PairElementCount = 10;
    internal const int StartElementCount = 4;
    internal const int StopElementCount = 3;
    internal const int CanonicalNarrowWidth = 1;
    internal const int CanonicalWideWidth = 3;
    internal const int StartPatternBits = 0x00;
    internal const int StopPatternBits = 0x04;

    static readonly int[] DigitPatternBits = new int[]
    {
        0x06,
        0x11,
        0x09,
        0x18,
        0x05,
        0x14,
        0x0C,
        0x03,
        0x12,
        0x0A
    };

    static readonly int[] StartWidths = new int[] { 1, 1, 1, 1 };
    static readonly int[] StopWidths = new int[] { 3, 1, 1 };

    internal static int GetDigitPatternBits(int digit)
    {
        ValidateDigit(digit);
        return DigitPatternBits[digit];
    }

    internal static int FindDigitForPatternBits(int patternBits)
    {
        int digit = 0;
        while (digit < DigitPatternBits.Length)
        {
            if (DigitPatternBits[digit] == patternBits)
            {
                return digit;
            }

            digit = digit + 1;
        }

        throw new BarcodeValidationException("Interleaved 2 of 5 pattern is not assigned to a digit.");
    }

    internal static bool[] GetDigitWideElements(int digit)
    {
        int patternBits = GetDigitPatternBits(digit);
        bool[] wideElements = new bool[DigitElementCount];
        int index = 0;
        while (index < DigitElementCount)
        {
            int bit = DigitElementCount - index - 1;
            wideElements[index] = (patternBits & (1 << bit)) != 0;
            index = index + 1;
        }

        return wideElements;
    }

    internal static int[] GetCanonicalDigitWidths(int digit)
    {
        bool[] wideElements = GetDigitWideElements(digit);
        int[] widths = new int[DigitElementCount];
        int index = 0;
        while (index < widths.Length)
        {
            widths[index] = wideElements[index]
                ? CanonicalWideWidth
                : CanonicalNarrowWidth;
            index = index + 1;
        }

        return widths;
    }

    internal static int[] GetCanonicalPairWidths(int barDigit, int spaceDigit)
    {
        int[] barWidths = GetCanonicalDigitWidths(barDigit);
        int[] spaceWidths = GetCanonicalDigitWidths(spaceDigit);
        int[] pairWidths = new int[PairElementCount];
        int index = 0;
        while (index < DigitElementCount)
        {
            pairWidths[index * 2] = barWidths[index];
            pairWidths[(index * 2) + 1] = spaceWidths[index];
            index = index + 1;
        }

        return pairWidths;
    }

    internal static int[] GetCanonicalStartWidths()
    {
        return CopyWidths(StartWidths);
    }

    internal static int[] GetCanonicalStopWidths()
    {
        return CopyWidths(StopWidths);
    }

    internal static bool[] GetStartWideElements()
    {
        return CreateWideElements(StartPatternBits, StartElementCount);
    }

    internal static bool[] GetStopWideElements()
    {
        return CreateWideElements(StopPatternBits, StopElementCount);
    }

    static bool[] CreateWideElements(int patternBits, int elementCount)
    {
        bool[] wideElements = new bool[elementCount];
        int index = 0;
        while (index < elementCount)
        {
            int bit = elementCount - index - 1;
            wideElements[index] = (patternBits & (1 << bit)) != 0;
            index = index + 1;
        }

        return wideElements;
    }

    static int[] CopyWidths(int[] source)
    {
        int[] copy = new int[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static void ValidateDigit(int digit)
    {
        if (digit < 0 || digit >= DigitCount)
        {
            throw new BarcodeValidationException("Interleaved 2 of 5 digit must be zero through nine.");
        }
    }
}
