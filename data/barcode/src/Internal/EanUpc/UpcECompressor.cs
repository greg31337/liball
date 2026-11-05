namespace barcode;

internal static class UpcECompressor
{
    internal const int CompressedCoreLength = 7;
    internal const int CompressedCompleteLength = 8;
    internal const int ExpandedDataLength = 11;
    internal const int ExpandedCompleteLength = 12;

    internal static string ExpandCompleteUpcE(string upcE)
    {
        ValidateAsciiDigits(upcE, CompressedCompleteLength, "UPC-E requires eight complete digits.");
        ValidateNumberSystem(upcE[0]);
        string expandedData = ExpandCore(upcE);
        int expectedCheckDigit = Gs1Modulo10.CalculateCheckDigit(expandedData);
        int observedCheckDigit = upcE[CompressedCompleteLength - 1] - '0';
        if (observedCheckDigit != expectedCheckDigit)
        {
            throw new BarcodeEncodingException("The supplied UPC-E check digit is invalid.");
        }

        return expandedData + upcE[CompressedCompleteLength - 1];
    }

    internal static string? TryCompressCompleteUpcA(string upcA)
    {
        ValidateAsciiDigits(upcA, ExpandedCompleteLength, "UPC-A compression requires twelve complete digits.");
        ValidateNumberSystem(upcA[0]);
        if (!Gs1Modulo10.VerifyCompleteDigits(upcA))
        {
            throw new BarcodeEncodingException("The supplied UPC-A check digit is invalid.");
        }

        string? compressedCore = TryCompressData(upcA);
        if (compressedCore == null)
        {
            return null;
        }

        return compressedCore + upcA[ExpandedCompleteLength - 1];
    }

    internal static string CompressCompleteUpcA(string upcA)
    {
        string? compressed = TryCompressCompleteUpcA(upcA);
        if (compressed == null)
        {
            throw new BarcodeEncodingException("The UPC-A value cannot be represented losslessly as UPC-E.");
        }

        return compressed;
    }

    internal static string AppendCheckDigitToCore(string upcECore)
    {
        ValidateAsciiDigits(upcECore, CompressedCoreLength, "UPC-E check calculation requires seven digits including the number system.");
        ValidateNumberSystem(upcECore[0]);
        string expandedData = ExpandCore(upcECore);
        int checkDigit = Gs1Modulo10.CalculateCheckDigit(expandedData);
        return upcECore + (char)('0' + checkDigit);
    }

    static string ExpandCore(string upcE)
    {
        char lastCompressedDigit = upcE[6];
        switch (lastCompressedDigit)
        {
            case '0':
            case '1':
            case '2':
                return upcE[0] + upcE.Substring(1, 2) + lastCompressedDigit + "0000" + upcE.Substring(3, 3);
            case '3':
                return upcE[0] + upcE.Substring(1, 3) + "00000" + upcE.Substring(4, 2);
            case '4':
                return upcE[0] + upcE.Substring(1, 4) + "00000" + upcE[5];
            default:
                return upcE[0] + upcE.Substring(1, 5) + "0000" + lastCompressedDigit;
        }
    }

    static string? TryCompressData(string upcA)
    {
        char numberSystem = upcA[0];
        if (upcA[4] == '0' && upcA[5] == '0' &&
            upcA[6] == '0' && upcA[7] == '0' &&
            upcA[3] >= '0' && upcA[3] <= '2')
        {
            return numberSystem + upcA.Substring(1, 2) + upcA.Substring(8, 3) + upcA[3];
        }

        if (upcA[4] == '0' && upcA[5] == '0' &&
            upcA[6] == '0' && upcA[7] == '0' && upcA[8] == '0')
        {
            return numberSystem + upcA.Substring(1, 3) + upcA.Substring(9, 2) + '3';
        }

        if (upcA[5] == '0' && upcA[6] == '0' &&
            upcA[7] == '0' && upcA[8] == '0' && upcA[9] == '0')
        {
            return numberSystem + upcA.Substring(1, 4) + upcA[10] + '4';
        }

        if (upcA[6] == '0' && upcA[7] == '0' &&
            upcA[8] == '0' && upcA[9] == '0' && upcA[10] >= '5')
        {
            return numberSystem + upcA.Substring(1, 5) + upcA[10];
        }

        return null;
    }

    static void ValidateAsciiDigits(string digits, int requiredLength, string lengthMessage)
    {
        if (digits == null || digits.Length != requiredLength)
        {
            throw new BarcodeEncodingException(lengthMessage);
        }

        int index = 0;
        while (index < digits.Length)
        {
            char character = digits[index];
            if (character < '0' || character > '9')
            {
                throw new BarcodeEncodingException("UPC-E conversion input must contain only ASCII digits.");
            }

            index = index + 1;
        }
    }

    static void ValidateNumberSystem(char numberSystem)
    {
        if (numberSystem != '0' && numberSystem != '1')
        {
            throw new BarcodeEncodingException("UPC-E number system must be zero or one.");
        }
    }
}
