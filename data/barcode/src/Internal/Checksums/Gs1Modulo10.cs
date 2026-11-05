namespace barcode;

internal static class Gs1Modulo10
{
    internal const string AlgorithmName = "GS1 modulo-10";

    internal static int CalculateCheckDigit(string digits)
    {
        ValidateDataDigits(digits);
        return CalculateCheckDigitForPrefix(digits, digits.Length);
    }

    internal static bool VerifyCompleteDigits(string digitsWithCheckDigit)
    {
        if (digitsWithCheckDigit == null)
        {
            throw new BarcodeValidationException("GS1 digits must not be null.");
        }

        if (digitsWithCheckDigit.Length < 2)
        {
            throw new BarcodeValidationException("GS1 verification requires data and a check digit.");
        }

        ValidateAsciiDigits(digitsWithCheckDigit);
        int dataLength = digitsWithCheckDigit.Length - 1;
        int expected = CalculateCheckDigitForPrefix(digitsWithCheckDigit, dataLength);
        int observed = digitsWithCheckDigit[dataLength] - '0';
        return observed == expected;
    }

    internal static bool VerifyCheckDigit(string digits, int observedCheckDigit)
    {
        ValidateDataDigits(digits);
        if (observedCheckDigit < 0 || observedCheckDigit > 9)
        {
            throw new BarcodeValidationException("GS1 check digit must be from zero through nine.");
        }

        return CalculateCheckDigitForPrefix(digits, digits.Length) == observedCheckDigit;
    }

    internal static string AppendCheckDigit(string digits)
    {
        int checkDigit = CalculateCheckDigit(digits);
        char checkCharacter = (char)('0' + checkDigit);
        return digits + checkCharacter;
    }

    internal static int CalculateCheckDigitForPrefix(string digits, int dataLength)
    {
        if (digits == null)
        {
            throw new BarcodeValidationException("GS1 digits must not be null.");
        }

        if (dataLength <= 0 || dataLength > digits.Length)
        {
            throw new BarcodeValidationException("GS1 data length is outside the supplied digits.");
        }

        int sumModuloTen = 0;
        int weight = 3;
        int index = dataLength - 1;
        while (index >= 0)
        {
            char character = digits[index];
            if (character < '0' || character > '9')
            {
                throw new BarcodeValidationException("GS1 checksum data must contain only ASCII digits.");
            }

            int digit = character - '0';
            sumModuloTen = (sumModuloTen + (digit * weight)) % 10;
            if (weight == 3)
            {
                weight = 1;
            }
            else
            {
                weight = 3;
            }

            index = index - 1;
        }

        return (10 - sumModuloTen) % 10;
    }

    internal static void ValidateDataDigits(string digits)
    {
        if (digits == null)
        {
            throw new BarcodeValidationException("GS1 digits must not be null.");
        }

        if (digits.Length == 0)
        {
            throw new BarcodeValidationException("GS1 checksum data must not be empty.");
        }

        ValidateAsciiDigits(digits);
    }

    internal static void ValidateAsciiDigits(string digits)
    {
        int index = 0;
        while (index < digits.Length)
        {
            char character = digits[index];
            if (character < '0' || character > '9')
            {
                throw new BarcodeValidationException("GS1 checksum data must contain only ASCII digits.");
            }

            index = index + 1;
        }
    }
}
