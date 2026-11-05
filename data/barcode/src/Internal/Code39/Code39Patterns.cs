namespace barcode;

internal static class Code39Patterns
{
    internal const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";
    internal const int DataCharacterCount = 43;
    internal const int ElementCount = 9;
    internal const int WideElementCount = 3;
    internal const int CanonicalNarrowWidth = 1;
    internal const int CanonicalWideWidth = 2;
    internal const char StartStopCharacter = '*';
    internal const int StartStopPatternBits = 0x094;

    static readonly int[] DataPatternBits = new int[]
    {
        0x034,
        0x121,
        0x061,
        0x160,
        0x031,
        0x130,
        0x070,
        0x025,
        0x124,
        0x064,
        0x109,
        0x049,
        0x148,
        0x019,
        0x118,
        0x058,
        0x00D,
        0x10C,
        0x04C,
        0x01C,
        0x103,
        0x043,
        0x142,
        0x013,
        0x112,
        0x052,
        0x007,
        0x106,
        0x046,
        0x016,
        0x181,
        0x0C1,
        0x1C0,
        0x091,
        0x190,
        0x0D0,
        0x085,
        0x184,
        0x0C4,
        0x0A8,
        0x0A2,
        0x08A,
        0x02A
    };

    internal static bool IsStandardDataCharacter(char character)
    {
        return Alphabet.IndexOf(character) >= 0;
    }

    internal static bool IsStartStop(char character)
    {
        return character == StartStopCharacter;
    }

    internal static void ValidateStandardText(string text)
    {
        if (text == null)
        {
            throw new BarcodeEncodingException("Code 39 text must not be null.");
        }

        if (text.Length == 0)
        {
            throw new BarcodeEncodingException("Code 39 text must not be empty.");
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!IsStandardDataCharacter(text[index]))
            {
                throw new BarcodeEncodingException("Code 39 text contains a character outside the standard alphabet.");
            }

            index = index + 1;
        }
    }

    internal static int GetValue(char character)
    {
        int value = Alphabet.IndexOf(character);
        if (value < 0)
        {
            throw new BarcodeValidationException("Code 39 data character is outside the standard alphabet.");
        }

        return value;
    }

    internal static char GetCharacter(int value)
    {
        ValidateValue(value);
        return Alphabet[value];
    }

    internal static int GetDataPatternBits(int value)
    {
        ValidateValue(value);
        return DataPatternBits[value];
    }

    internal static int GetCharacterPatternBits(char character)
    {
        if (character == StartStopCharacter)
        {
            return StartStopPatternBits;
        }

        return GetDataPatternBits(GetValue(character));
    }

    internal static bool[] GetWideElements(char character)
    {
        int patternBits = GetCharacterPatternBits(character);
        bool[] wideElements = new bool[ElementCount];
        int index = 0;
        while (index < ElementCount)
        {
            int bit = ElementCount - index - 1;
            wideElements[index] = (patternBits & (1 << bit)) != 0;
            index = index + 1;
        }

        return wideElements;
    }

    internal static int[] GetCanonicalWidths(char character)
    {
        bool[] wideElements = GetWideElements(character);
        int[] widths = new int[ElementCount];
        int index = 0;
        while (index < ElementCount)
        {
            widths[index] = wideElements[index]
                ? CanonicalWideWidth
                : CanonicalNarrowWidth;
            index = index + 1;
        }

        return widths;
    }

    internal static char FindCharacterForPatternBits(int patternBits)
    {
        if (patternBits == StartStopPatternBits)
        {
            return StartStopCharacter;
        }

        int index = 0;
        while (index < DataPatternBits.Length)
        {
            if (DataPatternBits[index] == patternBits)
            {
                return Alphabet[index];
            }

            index = index + 1;
        }

        throw new BarcodeValidationException("Code 39 wide-element pattern is not assigned.");
    }

    static void ValidateValue(int value)
    {
        if (value < 0 || value >= DataCharacterCount)
        {
            throw new BarcodeValidationException("Code 39 character value must be zero through 42.");
        }
    }
}
