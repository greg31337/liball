namespace barcode;

internal static class Code128Modulo103
{
    internal const string AlgorithmName = "Code 128 modulo-103";

    internal static int CalculateCheckCodeword(int[] codewords)
    {
        ValidateCodewordSequence(codewords);
        return CalculateCheckCodewordForPrefix(codewords, codewords.Length);
    }

    internal static bool VerifyCompleteCodewords(int[] codewordsWithCheck)
    {
        if (codewordsWithCheck == null)
        {
            throw new BarcodeValidationException("Code 128 codewords must not be null.");
        }

        if (codewordsWithCheck.Length < 2)
        {
            throw new BarcodeValidationException("Code 128 verification requires a start and check codeword.");
        }

        int dataLength = codewordsWithCheck.Length - 1;
        int observed = codewordsWithCheck[dataLength];
        ValidateCheckCodeword(observed);
        int expected = CalculateCheckCodewordForPrefix(codewordsWithCheck, dataLength);
        return observed == expected;
    }

    internal static bool VerifyCheckCodeword(int[] codewords, int observedCheckCodeword)
    {
        ValidateCodewordSequence(codewords);
        ValidateCheckCodeword(observedCheckCodeword);
        return CalculateCheckCodewordForPrefix(codewords, codewords.Length) == observedCheckCodeword;
    }

    internal static int[] AppendCheckCodeword(int[] codewords)
    {
        int checkCodeword = CalculateCheckCodeword(codewords);
        int[] complete = new int[CheckedMath.Add(codewords.Length, 1)];
        int index = 0;
        while (index < codewords.Length)
        {
            complete[index] = codewords[index];
            index = index + 1;
        }

        complete[codewords.Length] = checkCodeword;
        return complete;
    }

    internal static int CalculateCheckCodewordForPrefix(int[] codewords, int dataLength)
    {
        if (codewords == null)
        {
            throw new BarcodeValidationException("Code 128 codewords must not be null.");
        }

        if (dataLength <= 0 || dataLength > codewords.Length)
        {
            throw new BarcodeValidationException("Code 128 data length is outside the supplied codewords.");
        }

        ValidateStartCodeword(codewords[0]);
        int sumModulo103 = codewords[0] % 103;
        int index = 1;
        while (index < dataLength)
        {
            int codeword = codewords[index];
            ValidateDataCodeword(codeword);
            int weight = index % 103;
            sumModulo103 = (sumModulo103 + (codeword * weight)) % 103;
            index = index + 1;
        }

        return sumModulo103;
    }

    internal static void ValidateCodewordSequence(int[] codewords)
    {
        if (codewords == null)
        {
            throw new BarcodeValidationException("Code 128 codewords must not be null.");
        }

        if (codewords.Length == 0)
        {
            throw new BarcodeValidationException("Code 128 codewords must include a start codeword.");
        }

        CalculateCheckCodewordForPrefix(codewords, codewords.Length);
    }

    internal static void ValidateStartCodeword(int codeword)
    {
        if (codeword < 103 || codeword > 105)
        {
            throw new BarcodeValidationException("Code 128 start codeword must be Start A, Start B, or Start C.");
        }
    }

    internal static void ValidateDataCodeword(int codeword)
    {
        if (codeword < 0 || codeword > 102)
        {
            throw new BarcodeValidationException("Code 128 data codeword must be from zero through 102.");
        }
    }

    internal static void ValidateCheckCodeword(int codeword)
    {
        if (codeword < 0 || codeword > 102)
        {
            throw new BarcodeValidationException("Code 128 check codeword must be from zero through 102.");
        }
    }
}
