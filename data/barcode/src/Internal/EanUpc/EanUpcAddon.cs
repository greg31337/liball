namespace barcode;

internal class EanUpcAddonAssociation
{
    internal EanUpcEncodedData Main = new EanUpcEncodedData();
    internal EanUpcEncodedData? AddOn;
}

internal static class EanUpcAddon
{
    internal const int TwoDigitLength = 2;
    internal const int FiveDigitLength = 5;
    internal const int TwoDigitModuleCount = 20;
    internal const int FiveDigitModuleCount = 47;

    internal static EanUpcEncodedData Encode(string digits)
    {
        int[] digitValues = ValidateAndConvertDigits(digits);
        EanUpcDigitSet[] parity;
        BarcodeFormat format;
        int parityValue;
        int moduleCount;
        if (digitValues.Length == TwoDigitLength)
        {
            parityValue = ((digitValues[0] * 10) + digitValues[1]) % 4;
            parity = EanUpcPatterns.GetAddon2Parity(parityValue);
            format = BarcodeFormat.EanUpcAddon2;
            moduleCount = TwoDigitModuleCount;
        }
        else
        {
            parityValue = CalculateFiveDigitChecksum(digitValues);
            parity = EanUpcPatterns.GetAddon5Parity(parityValue);
            format = BarcodeFormat.EanUpcAddon5;
            moduleCount = FiveDigitModuleCount;
        }

        byte[] modules = EncodeModules(digitValues, parity, moduleCount);
        EanUpcEncodedData result = new EanUpcEncodedData();
        result.Format = format;
        result.Text = digits;
        result.Digits = digitValues;
        result.LeftParity = parity;
        result.Modules = modules;
        result.ChecksumValue = parityValue;
        result.ChecksumCalculated = true;
        return result;
    }

    internal static EanUpcAddonAssociation Associate(
        EanUpcEncodedData main,
        EanUpcOptions? options)
    {
        ValidateMainSymbol(main);
        int[]? allowedLengths = options == null ? null : options.AllowedAddOnLengths;
        ValidateAllowedLengths(allowedLengths);

        EanUpcAddonAssociation association = new EanUpcAddonAssociation();
        association.Main = main;
        if (options == null || options.AddOn == null)
        {
            if (options != null && options.RequireAddOn)
            {
                throw new BarcodeEncodingException("An EAN/UPC add-on is required.");
            }

            return association;
        }

        EanUpcEncodedData addOn = Encode(options.AddOn);
        if (!IsAllowedLength(addOn.Text.Length, allowedLengths))
        {
            throw new BarcodeEncodingException("The EAN/UPC add-on length is not allowed by the selected policy.");
        }

        association.AddOn = addOn;
        return association;
    }

    static int[] ValidateAndConvertDigits(string digits)
    {
        if (digits == null)
        {
            throw new BarcodeEncodingException("EAN/UPC add-on digits must not be null.");
        }

        if (digits.Length != TwoDigitLength && digits.Length != FiveDigitLength)
        {
            throw new BarcodeEncodingException("An EAN/UPC add-on must contain exactly two or five digits.");
        }

        int[] values = new int[digits.Length];
        int index = 0;
        while (index < digits.Length)
        {
            char character = digits[index];
            if (character < '0' || character > '9')
            {
                throw new BarcodeEncodingException("EAN/UPC add-on input must contain only ASCII digits.");
            }

            values[index] = character - '0';
            index = index + 1;
        }

        return values;
    }

    static int CalculateFiveDigitChecksum(int[] digits)
    {
        int oddPositionSum = digits[0] + digits[2] + digits[4];
        int evenPositionSum = digits[1] + digits[3];
        return ((oddPositionSum * 3) + (evenPositionSum * 9)) % 10;
    }

    static byte[] EncodeModules(
        int[] digits,
        EanUpcDigitSet[] parity,
        int moduleCount)
    {
        byte[] modules = new byte[moduleCount];
        int offset = WriteModules(
            modules,
            0,
            EanUpcPatterns.GetGuardModules(EanUpcGuardPattern.AddonStart));
        int index = 0;
        while (index < digits.Length)
        {
            if (index > 0)
            {
                offset = WriteModules(
                    modules,
                    offset,
                    EanUpcPatterns.GetGuardModules(EanUpcGuardPattern.AddonSeparator));
            }

            offset = WriteModules(
                modules,
                offset,
                EanUpcPatterns.GetDigitModules(digits[index], parity[index]));
            index = index + 1;
        }

        return modules;
    }

    static void ValidateMainSymbol(EanUpcEncodedData main)
    {
        if (main == null)
        {
            throw new BarcodeEncodingException("EAN/UPC add-on association requires a main symbol.");
        }

        int requiredModuleCount;
        switch (main.Format)
        {
            case BarcodeFormat.Ean13:
                requiredModuleCount = EanUpcEncoder.Ean13ModuleCount;
                break;
            case BarcodeFormat.UpcA:
                requiredModuleCount = EanUpcEncoder.UpcAModuleCount;
                break;
            case BarcodeFormat.UpcE:
                requiredModuleCount = EanUpcEncoder.UpcEModuleCount;
                break;
            default:
                throw new BarcodeEncodingException("EAN/UPC add-ons may be associated only with EAN-13, UPC-A, or UPC-E.");
        }

        if (main.Modules == null || main.Modules.Length != requiredModuleCount ||
            main.Text == null || main.Text.Length == 0)
        {
            throw new BarcodeEncodingException("The main EAN/UPC symbol is incomplete.");
        }
    }

    static void ValidateAllowedLengths(int[]? allowedLengths)
    {
        if (allowedLengths == null)
        {
            return;
        }

        int index = 0;
        while (index < allowedLengths.Length)
        {
            int length = allowedLengths[index];
            if (length != TwoDigitLength && length != FiveDigitLength)
            {
                throw new BarcodeEncodingException("Allowed EAN/UPC add-on lengths may contain only two and five.");
            }

            index = index + 1;
        }
    }

    static bool IsAllowedLength(int length, int[]? allowedLengths)
    {
        if (allowedLengths == null)
        {
            return true;
        }

        int index = 0;
        while (index < allowedLengths.Length)
        {
            if (allowedLengths[index] == length)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
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
