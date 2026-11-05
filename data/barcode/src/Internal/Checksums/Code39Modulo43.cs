namespace barcode;

internal static class Code39Modulo43
{
    internal const string AlgorithmName = "Code 39 modulo-43";
    internal const string Alphabet = Code39Patterns.Alphabet;

    internal static int CalculateCheckValue(string text)
    {
        ValidateDataText(text);
        return CalculateCheckValueForPrefix(text, text.Length);
    }

    internal static char CalculateCheckCharacter(string text)
    {
        int value = CalculateCheckValue(text);
        return Alphabet[value];
    }

    internal static bool VerifyCompleteText(string textWithCheckCharacter)
    {
        if (textWithCheckCharacter == null)
        {
            throw new BarcodeValidationException("Code 39 text must not be null.");
        }

        if (textWithCheckCharacter.Length < 2)
        {
            throw new BarcodeValidationException("Code 39 verification requires data and a check character.");
        }

        int dataLength = textWithCheckCharacter.Length - 1;
        int expected = CalculateCheckValueForPrefix(textWithCheckCharacter, dataLength);
        int observed = FindCharacterValue(textWithCheckCharacter[dataLength]);
        return observed == expected;
    }

    internal static bool VerifyCheckCharacter(string text, char observedCheckCharacter)
    {
        ValidateDataText(text);
        int observed = FindCharacterValue(observedCheckCharacter);
        return CalculateCheckValueForPrefix(text, text.Length) == observed;
    }

    internal static string AppendCheckCharacter(string text)
    {
        char checkCharacter = CalculateCheckCharacter(text);
        return text + checkCharacter;
    }

    internal static int CalculateCheckValueForPrefix(string text, int dataLength)
    {
        if (text == null)
        {
            throw new BarcodeValidationException("Code 39 text must not be null.");
        }

        if (dataLength <= 0 || dataLength > text.Length)
        {
            throw new BarcodeValidationException("Code 39 data length is outside the supplied text.");
        }

        int sumModulo43 = 0;
        int index = 0;
        while (index < dataLength)
        {
            int value = FindCharacterValue(text[index]);
            sumModulo43 = (sumModulo43 + value) % 43;
            index = index + 1;
        }

        return sumModulo43;
    }

    internal static int FindCharacterValue(char character)
    {
        int value = Alphabet.IndexOf(character);
        if (value < 0)
        {
            throw new BarcodeValidationException("Code 39 checksum data contains a character outside the standard alphabet.");
        }

        return value;
    }

    internal static void ValidateDataText(string text)
    {
        if (text == null)
        {
            throw new BarcodeValidationException("Code 39 text must not be null.");
        }

        if (text.Length == 0)
        {
            throw new BarcodeValidationException("Code 39 checksum data must not be empty.");
        }

        CalculateCheckValueForPrefix(text, text.Length);
    }
}
