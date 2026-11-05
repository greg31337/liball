namespace barcode;

internal static class CodabarModulo16
{
    internal const string AlgorithmName = "Codabar modulo-16";
    internal const string CharacterValues = "0123456789-$:/.+ABCD";
    internal const string DataCharacters = "0123456789-$:/.+";
    internal const string StartStopCharacters = "ABCD";

    internal static int CalculateCheckValue(string framedText)
    {
        ValidateFramedText(framedText);
        int sumModulo16 = CalculateFramedSumModulo16(framedText, framedText.Length);
        return (16 - sumModulo16) % 16;
    }

    internal static char CalculateCheckCharacter(string framedText)
    {
        int value = CalculateCheckValue(framedText);
        return DataCharacters[value];
    }

    internal static bool VerifyCompleteText(string framedTextWithCheckCharacter)
    {
        ValidateCompleteText(framedTextWithCheckCharacter);
        int sumModulo16 = CalculateFramedSumModulo16(framedTextWithCheckCharacter, framedTextWithCheckCharacter.Length);
        return sumModulo16 == 0;
    }

    internal static bool VerifyCheckCharacter(string framedText, char observedCheckCharacter)
    {
        ValidateFramedText(framedText);
        int observed = FindDataCharacterValue(observedCheckCharacter);
        return CalculateCheckValue(framedText) == observed;
    }

    internal static string AppendCheckCharacter(string framedText)
    {
        char checkCharacter = CalculateCheckCharacter(framedText);
        int stopIndex = framedText.Length - 1;
        return framedText.Substring(0, stopIndex) + checkCharacter + framedText[stopIndex];
    }

    internal static int CalculateFramedSumModulo16(string framedText, int textLength)
    {
        if (framedText == null)
        {
            throw new BarcodeValidationException("Codabar text must not be null.");
        }

        if (textLength <= 0 || textLength > framedText.Length)
        {
            throw new BarcodeValidationException("Codabar text length is outside the supplied text.");
        }

        int sumModulo16 = 0;
        int index = 0;
        while (index < textLength)
        {
            int value = CharacterValues.IndexOf(framedText[index]);
            if (value < 0)
            {
                throw new BarcodeValidationException("Codabar checksum data contains an unsupported character.");
            }

            sumModulo16 = (sumModulo16 + value) % 16;
            index = index + 1;
        }

        return sumModulo16;
    }

    internal static int FindDataCharacterValue(char character)
    {
        int value = DataCharacters.IndexOf(character);
        if (value < 0)
        {
            throw new BarcodeValidationException("Codabar check character must belong to the data alphabet.");
        }

        return value;
    }

    internal static void ValidateFramedText(string framedText)
    {
        if (framedText == null)
        {
            throw new BarcodeValidationException("Codabar text must not be null.");
        }

        if (framedText.Length < 3)
        {
            throw new BarcodeValidationException("Codabar checksum data requires start, data, and stop characters.");
        }

        int stopIndex = framedText.Length - 1;
        if (StartStopCharacters.IndexOf(framedText[0]) < 0 || StartStopCharacters.IndexOf(framedText[stopIndex]) < 0)
        {
            throw new BarcodeValidationException("Codabar checksum data must begin and end with A, B, C, or D.");
        }

        int index = 1;
        while (index < stopIndex)
        {
            if (DataCharacters.IndexOf(framedText[index]) < 0)
            {
                throw new BarcodeValidationException("Codabar checksum data contains an unsupported data character.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateCompleteText(string framedTextWithCheckCharacter)
    {
        if (framedTextWithCheckCharacter == null)
        {
            throw new BarcodeValidationException("Codabar text must not be null.");
        }

        if (framedTextWithCheckCharacter.Length < 4)
        {
            throw new BarcodeValidationException("Codabar verification requires start, data, check, and stop characters.");
        }

        ValidateFramedText(framedTextWithCheckCharacter);
    }
}
