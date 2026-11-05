namespace barcode;

internal class EanUpcEncodedData
{
    internal BarcodeFormat Format;
    internal string Text = "";
    internal string Ean13CompatibleText = "";
    internal string ExpandedUpcAText = "";
    internal int[] Digits = new int[0];
    internal EanUpcDigitSet[] LeftParity = new EanUpcDigitSet[0];
    internal byte[] Modules = new byte[0];
    internal int ChecksumValue;
    internal bool ChecksumCalculated;
}

internal static class EanUpcEncoder
{
    internal const int Ean13DataLength = 12;
    internal const int Ean13CompleteLength = 13;
    internal const int Ean13ModuleCount = 95;
    internal const int Ean8DataLength = 7;
    internal const int Ean8CompleteLength = 8;
    internal const int Ean8ModuleCount = 67;
    internal const int UpcADataLength = 11;
    internal const int UpcACompleteLength = 12;
    internal const int UpcAModuleCount = Ean13ModuleCount;
    internal const int UpcEPayloadLength = 6;
    internal const int UpcECoreLength = 7;
    internal const int UpcECompleteLength = 8;
    internal const int UpcEModuleCount = 51;

    internal static EanUpcEncodedData Encode(
        string digits,
        BarcodeFormat format,
        BarcodeChecksumMode checksumMode)
    {
        ValidateChecksumMode(checksumMode);
        int dataLength;
        int completeLength;
        switch (format)
        {
            case BarcodeFormat.Ean13:
                dataLength = Ean13DataLength;
                completeLength = Ean13CompleteLength;
                break;
            case BarcodeFormat.Ean8:
                dataLength = Ean8DataLength;
                completeLength = Ean8CompleteLength;
                break;
            case BarcodeFormat.UpcA:
                dataLength = UpcADataLength;
                completeLength = UpcACompleteLength;
                break;
            default:
                throw new BarcodeEncodingException("EAN/UPC encoder requires EAN-13, EAN-8, or UPC-A format.");
        }

        string completeDigits = ResolveCompleteDigits(
            digits,
            dataLength,
            completeLength,
            checksumMode,
            format);
        bool checksumCalculated = digits != null && digits.Length == dataLength;
        int[] digitValues = ConvertDigits(completeDigits);

        EanUpcDigitSet[] leftParity;
        byte[] modules;
        string ean13CompatibleText = "";
        if (format == BarcodeFormat.Ean13)
        {
            ean13CompatibleText = completeDigits;
            leftParity = EanUpcPatterns.GetEan13Parity(digitValues[0]);
            modules = EncodeEan13Modules(digitValues, leftParity);
        }
        else if (format == BarcodeFormat.UpcA)
        {
            ean13CompatibleText = "0" + completeDigits;
            int[] compatibleDigits = ConvertDigits(ean13CompatibleText);
            leftParity = EanUpcPatterns.GetEan13Parity(0);
            modules = EncodeEan13Modules(compatibleDigits, leftParity);
        }
        else
        {
            leftParity = CreateEan8LeftParity();
            modules = EncodeEan8Modules(digitValues, leftParity);
        }

        EanUpcEncodedData result = new EanUpcEncodedData();
        result.Format = format;
        result.Text = completeDigits;
        result.Ean13CompatibleText = ean13CompatibleText;
        if (format == BarcodeFormat.UpcA)
        {
            result.ExpandedUpcAText = completeDigits;
        }

        result.Digits = digitValues;
        result.LeftParity = leftParity;
        result.Modules = modules;
        result.ChecksumValue = digitValues[digitValues.Length - 1];
        result.ChecksumCalculated = checksumCalculated;
        return result;
    }

    internal static EanUpcEncodedData EncodeUpcE(
        string digits,
        int numberSystem,
        BarcodeChecksumMode checksumMode)
    {
        ValidateChecksumMode(checksumMode);
        if (numberSystem != 0 && numberSystem != 1)
        {
            throw new BarcodeEncodingException("UPC-E number system must be zero or one.");
        }

        string completeDigits = ResolveCompleteUpcE(digits, numberSystem, checksumMode);
        bool checksumCalculated = digits != null && digits.Length < UpcECompleteLength;
        string expandedUpcA = UpcECompressor.ExpandCompleteUpcE(completeDigits);
        int[] digitValues = ConvertDigits(completeDigits);
        int checkDigit = digitValues[UpcECompleteLength - 1];
        EanUpcDigitSet[] parity = EanUpcPatterns.GetUpcEParity(numberSystem, checkDigit);
        byte[] modules = EncodeUpcEModules(digitValues, parity);

        EanUpcEncodedData result = new EanUpcEncodedData();
        result.Format = BarcodeFormat.UpcE;
        result.Text = completeDigits;
        result.Ean13CompatibleText = "0" + expandedUpcA;
        result.ExpandedUpcAText = expandedUpcA;
        result.Digits = digitValues;
        result.LeftParity = parity;
        result.Modules = modules;
        result.ChecksumValue = checkDigit;
        result.ChecksumCalculated = checksumCalculated;
        return result;
    }

    static string ResolveCompleteDigits(
        string digits,
        int dataLength,
        int completeLength,
        BarcodeChecksumMode checksumMode,
        BarcodeFormat format)
    {
        if (digits == null)
        {
            throw new BarcodeEncodingException("EAN/UPC digits must not be null.");
        }

        ValidateAsciiDigits(digits);
        if (digits.Length == dataLength)
        {
            if (checksumMode == BarcodeChecksumMode.Require ||
                checksumMode == BarcodeChecksumMode.VerifyKeep ||
                checksumMode == BarcodeChecksumMode.VerifyStrip)
            {
                throw new BarcodeEncodingException("The selected EAN/UPC checksum mode requires a supplied check digit.");
            }

            return Gs1Modulo10.AppendCheckDigit(digits);
        }

        if (digits.Length == completeLength)
        {
            if (!Gs1Modulo10.VerifyCompleteDigits(digits))
            {
                throw new BarcodeEncodingException("The supplied EAN/UPC check digit is invalid.");
            }

            return digits;
        }

        if (format == BarcodeFormat.Ean13)
        {
            throw new BarcodeEncodingException("EAN-13 requires 12 data digits or 13 complete digits.");
        }

        if (format == BarcodeFormat.UpcA)
        {
            throw new BarcodeEncodingException("UPC-A requires 11 data digits or 12 complete digits.");
        }

        throw new BarcodeEncodingException("EAN-8 requires 7 data digits or 8 complete digits.");
    }

    static string ResolveCompleteUpcE(
        string digits,
        int numberSystem,
        BarcodeChecksumMode checksumMode)
    {
        if (digits == null)
        {
            throw new BarcodeEncodingException("UPC-E digits must not be null.");
        }

        ValidateAsciiDigits(digits);
        string core;
        if (digits.Length == UpcEPayloadLength)
        {
            core = (char)('0' + numberSystem) + digits;
        }
        else if (digits.Length == UpcECoreLength)
        {
            ValidateUpcENumberSystemDigit(digits[0], numberSystem);
            core = digits;
        }
        else if (digits.Length == UpcECompleteLength)
        {
            ValidateUpcENumberSystemDigit(digits[0], numberSystem);
            UpcECompressor.ExpandCompleteUpcE(digits);
            return digits;
        }
        else
        {
            throw new BarcodeEncodingException("UPC-E requires six payload digits, seven digits including number system, or eight complete digits.");
        }

        if (checksumMode == BarcodeChecksumMode.Require ||
            checksumMode == BarcodeChecksumMode.VerifyKeep ||
            checksumMode == BarcodeChecksumMode.VerifyStrip)
        {
            throw new BarcodeEncodingException("The selected EAN/UPC checksum mode requires a supplied check digit.");
        }

        return UpcECompressor.AppendCheckDigitToCore(core);
    }

    static void ValidateUpcENumberSystemDigit(char numberSystemDigit, int expectedNumberSystem)
    {
        char expected = (char)('0' + expectedNumberSystem);
        if (numberSystemDigit != expected)
        {
            throw new BarcodeEncodingException("UPC-E input number system does not match the requested number system.");
        }
    }

    static void ValidateAsciiDigits(string digits)
    {
        int index = 0;
        while (index < digits.Length)
        {
            char character = digits[index];
            if (character < '0' || character > '9')
            {
                throw new BarcodeEncodingException("EAN/UPC input must contain only ASCII digits.");
            }

            index = index + 1;
        }
    }

    static void ValidateChecksumMode(BarcodeChecksumMode checksumMode)
    {
        switch (checksumMode)
        {
            case BarcodeChecksumMode.None:
            case BarcodeChecksumMode.Calculate:
            case BarcodeChecksumMode.Require:
            case BarcodeChecksumMode.VerifyKeep:
            case BarcodeChecksumMode.VerifyStrip:
                return;
            default:
                throw new BarcodeEncodingException("EAN/UPC checksum mode is invalid.");
        }
    }

    static int[] ConvertDigits(string digits)
    {
        int[] values = new int[digits.Length];
        int index = 0;
        while (index < digits.Length)
        {
            values[index] = digits[index] - '0';
            index = index + 1;
        }

        return values;
    }

    static byte[] EncodeEan13Modules(int[] digits, EanUpcDigitSet[] leftParity)
    {
        byte[] modules = new byte[Ean13ModuleCount];
        int offset = WriteModules(
            modules,
            0,
            EanUpcPatterns.GetGuardModules(EanUpcGuardPattern.Normal));
        int index = 1;
        while (index <= 6)
        {
            offset = WriteModules(
                modules,
                offset,
                EanUpcPatterns.GetDigitModules(digits[index], leftParity[index - 1]));
            index = index + 1;
        }

        offset = WriteModules(
            modules,
            offset,
            EanUpcPatterns.GetGuardModules(EanUpcGuardPattern.Center));
        index = 7;
        while (index < digits.Length)
        {
            offset = WriteModules(
                modules,
                offset,
                EanUpcPatterns.GetDigitModules(digits[index], EanUpcDigitSet.R));
            index = index + 1;
        }

        WriteModules(
            modules,
            offset,
            EanUpcPatterns.GetGuardModules(EanUpcGuardPattern.Normal));
        return modules;
    }

    static byte[] EncodeEan8Modules(int[] digits, EanUpcDigitSet[] leftParity)
    {
        byte[] modules = new byte[Ean8ModuleCount];
        int offset = WriteModules(
            modules,
            0,
            EanUpcPatterns.GetGuardModules(EanUpcGuardPattern.Normal));
        int index = 0;
        while (index < 4)
        {
            offset = WriteModules(
                modules,
                offset,
                EanUpcPatterns.GetDigitModules(digits[index], leftParity[index]));
            index = index + 1;
        }

        offset = WriteModules(
            modules,
            offset,
            EanUpcPatterns.GetGuardModules(EanUpcGuardPattern.Center));
        index = 4;
        while (index < digits.Length)
        {
            offset = WriteModules(
                modules,
                offset,
                EanUpcPatterns.GetDigitModules(digits[index], EanUpcDigitSet.R));
            index = index + 1;
        }

        WriteModules(
            modules,
            offset,
            EanUpcPatterns.GetGuardModules(EanUpcGuardPattern.Normal));
        return modules;
    }

    static byte[] EncodeUpcEModules(int[] digits, EanUpcDigitSet[] parity)
    {
        byte[] modules = new byte[UpcEModuleCount];
        int offset = WriteModules(
            modules,
            0,
            EanUpcPatterns.GetGuardModules(EanUpcGuardPattern.Normal));
        int index = 1;
        while (index <= UpcEPayloadLength)
        {
            offset = WriteModules(
                modules,
                offset,
                EanUpcPatterns.GetDigitModules(digits[index], parity[index - 1]));
            index = index + 1;
        }

        WriteModules(
            modules,
            offset,
            EanUpcPatterns.GetGuardModules(EanUpcGuardPattern.UpcEEnd));
        return modules;
    }

    static EanUpcDigitSet[] CreateEan8LeftParity()
    {
        return new EanUpcDigitSet[]
        {
            EanUpcDigitSet.L,
            EanUpcDigitSet.L,
            EanUpcDigitSet.L,
            EanUpcDigitSet.L
        };
    }

    static int WriteModules(byte[] destination, int offset, byte[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[offset + index] = source[index];
            index = index + 1;
        }

        return CheckedMath.Add(offset, source.Length);
    }
}
