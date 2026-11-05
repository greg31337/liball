namespace barcode;

internal static class CodabarPatterns
{
    internal const string Alphabet = "0123456789-$:/.+ABCD";
    internal const string DataCharacters = "0123456789-$:/.+";
    internal const string StartStopCharacters = "ABCD";
    internal const string StartStopAliases = "TN*E";
    internal const int CharacterCount = 20;
    internal const int DataCharacterCount = 16;
    internal const int StartStopCharacterCount = 4;
    internal const int ElementCount = 7;
    internal const int BarElementCount = 4;
    internal const int SpaceElementCount = 3;
    internal const int MinimumWideElementCount = 2;
    internal const int MaximumWideElementCount = 3;
    internal const int CanonicalNarrowWidth = 1;
    internal const int CanonicalWideWidth = 2;

    static readonly int[] CharacterPatternBits = new int[]
    {
        0x03,
        0x06,
        0x09,
        0x60,
        0x12,
        0x42,
        0x21,
        0x24,
        0x30,
        0x48,
        0x0C,
        0x18,
        0x45,
        0x51,
        0x54,
        0x15,
        0x1A,
        0x29,
        0x0B,
        0x0E
    };

    internal static bool IsDataCharacter(char character)
    {
        return DataCharacters.IndexOf(character) >= 0;
    }

    internal static bool IsStartStopCharacter(char character)
    {
        return StartStopCharacters.IndexOf(character) >= 0;
    }

    internal static bool IsStartStopAlias(char character)
    {
        return StartStopAliases.IndexOf(character) >= 0;
    }

    internal static bool IsStartStopInput(char character, bool acceptAliases)
    {
        if (IsStartStopCharacter(character))
        {
            return true;
        }

        return acceptAliases && IsStartStopAlias(character);
    }

    internal static void ValidateDataText(string text)
    {
        if (text == null)
        {
            throw new BarcodeEncodingException("Codabar text must not be null.");
        }

        if (text.Length == 0)
        {
            throw new BarcodeEncodingException("Codabar text must not be empty.");
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!IsDataCharacter(text[index]))
            {
                throw new BarcodeEncodingException("Codabar text contains a character outside the data alphabet.");
            }

            index = index + 1;
        }
    }

    internal static int GetValue(char character)
    {
        int value = Alphabet.IndexOf(character);
        if (value < 0)
        {
            throw new BarcodeValidationException("Codabar character is outside the canonical alphabet.");
        }

        return value;
    }

    internal static char GetCharacter(int value)
    {
        ValidateValue(value);
        return Alphabet[value];
    }

    internal static int GetCharacterPatternBits(char character)
    {
        return CharacterPatternBits[GetValue(character)];
    }

    internal static int GetInputPatternBits(char character, bool acceptAliases)
    {
        char canonical = character;
        if (!IsDataCharacter(character))
        {
            canonical = NormalizeStartStop(character, acceptAliases);
        }

        return GetCharacterPatternBits(canonical);
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
        int index = 0;
        while (index < CharacterPatternBits.Length)
        {
            if (CharacterPatternBits[index] == patternBits)
            {
                return Alphabet[index];
            }

            index = index + 1;
        }

        throw new BarcodeValidationException("Codabar wide-element pattern is not assigned.");
    }

    internal static char NormalizeStartStop(char character, bool acceptAliases)
    {
        int canonicalIndex = StartStopCharacters.IndexOf(character);
        if (canonicalIndex >= 0)
        {
            return character;
        }

        int aliasIndex = StartStopAliases.IndexOf(character);
        if (acceptAliases && aliasIndex >= 0)
        {
            return StartStopCharacters[aliasIndex];
        }

        throw new BarcodeValidationException("Codabar start or stop character is not permitted.");
    }

    internal static char GetStartStopAlias(char canonicalCharacter)
    {
        int index = StartStopCharacters.IndexOf(canonicalCharacter);
        if (index < 0)
        {
            throw new BarcodeValidationException("Codabar alias lookup requires A, B, C, or D.");
        }

        return StartStopAliases[index];
    }

    static void ValidateValue(int value)
    {
        if (value < 0 || value >= CharacterCount)
        {
            throw new BarcodeValidationException("Codabar character value must be zero through 19.");
        }
    }
}
