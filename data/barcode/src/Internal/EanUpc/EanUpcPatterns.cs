namespace barcode;

internal enum EanUpcDigitSet
{
    L = 0,
    G = 1,
    R = 2
}

internal enum EanUpcGuardPattern
{
    Normal = 0,
    Center = 1,
    UpcEEnd = 2,
    AddonStart = 3,
    AddonSeparator = 4
}

internal static class EanUpcPatterns
{
    internal const int DigitCount = 10;
    internal const int DigitModuleCount = 7;
    internal const int DigitElementCount = 4;
    internal const int Ean13ParityLength = 6;
    internal const int UpcEParityLength = 6;
    internal const int Addon2ParityLength = 2;
    internal const int Addon5ParityLength = 5;

    const string NormalGuardModules = "101";
    const string CenterGuardModules = "01010";
    const string UpcEEndGuardModules = "010101";
    const string AddonStartGuardModules = "1011";
    const string AddonSeparatorModules = "01";

    static readonly string[] LDigitModules = new string[]
    {
        "0001101",
        "0011001",
        "0010011",
        "0111101",
        "0100011",
        "0110001",
        "0101111",
        "0111011",
        "0110111",
        "0001011"
    };

    static readonly string[] GDigitModules = new string[]
    {
        "0100111",
        "0110011",
        "0011011",
        "0100001",
        "0011101",
        "0111001",
        "0000101",
        "0010001",
        "0001001",
        "0010111"
    };

    static readonly string[] RDigitModules = new string[]
    {
        "1110010",
        "1100110",
        "1101100",
        "1000010",
        "1011100",
        "1001110",
        "1010000",
        "1000100",
        "1001000",
        "1110100"
    };

    static readonly string[] Ean13ParityPatterns = new string[]
    {
        "LLLLLL",
        "LLGLGG",
        "LLGGLG",
        "LLGGGL",
        "LGLLGG",
        "LGGLLG",
        "LGGGLL",
        "LGLGLG",
        "LGLGGL",
        "LGGLGL"
    };

    static readonly string[] UpcENumberSystemZeroParityPatterns = new string[]
    {
        "GGGLLL",
        "GGLGLL",
        "GGLLGL",
        "GGLLLG",
        "GLGGLL",
        "GLLGGL",
        "GLLLGG",
        "GLGLGL",
        "GLGLLG",
        "GLLGLG"
    };

    static readonly string[] UpcENumberSystemOneParityPatterns = new string[]
    {
        "LLLGGG",
        "LLGLGG",
        "LLGGLG",
        "LLGGGL",
        "LGLLGG",
        "LGGLLG",
        "LGGGLL",
        "LGLGLG",
        "LGLGGL",
        "LGGLGL"
    };

    static readonly string[] Addon2ParityPatterns = new string[]
    {
        "LL",
        "LG",
        "GL",
        "GG"
    };

    static readonly string[] Addon5ParityPatterns = new string[]
    {
        "GGLLL",
        "GLGLL",
        "GLLGL",
        "GLLLG",
        "LGGLL",
        "LLGGL",
        "LLLGG",
        "LGLGL",
        "LGLLG",
        "LLGLG"
    };

    internal static byte[] GetDigitModules(int digit, EanUpcDigitSet digitSet)
    {
        ValidateDigit(digit);
        string pattern = SelectDigitPattern(digit, digitSet);
        return ConvertModules(pattern);
    }

    internal static int[] GetDigitWidths(int digit, EanUpcDigitSet digitSet)
    {
        ValidateDigit(digit);
        string pattern = SelectDigitPattern(digit, digitSet);
        int[] widths = new int[DigitElementCount];
        char activeModule = pattern[0];
        int widthIndex = 0;
        int moduleIndex = 0;
        while (moduleIndex < pattern.Length)
        {
            char module = pattern[moduleIndex];
            if (module != activeModule)
            {
                widthIndex = widthIndex + 1;
                activeModule = module;
            }

            widths[widthIndex] = widths[widthIndex] + 1;
            moduleIndex = moduleIndex + 1;
        }

        return widths;
    }

    internal static byte[] GetGuardModules(EanUpcGuardPattern guardPattern)
    {
        string pattern;
        switch (guardPattern)
        {
            case EanUpcGuardPattern.Normal:
                pattern = NormalGuardModules;
                break;
            case EanUpcGuardPattern.Center:
                pattern = CenterGuardModules;
                break;
            case EanUpcGuardPattern.UpcEEnd:
                pattern = UpcEEndGuardModules;
                break;
            case EanUpcGuardPattern.AddonStart:
                pattern = AddonStartGuardModules;
                break;
            case EanUpcGuardPattern.AddonSeparator:
                pattern = AddonSeparatorModules;
                break;
            default:
                throw new BarcodeValidationException("EAN/UPC guard pattern is invalid.");
        }

        return ConvertModules(pattern);
    }

    internal static EanUpcDigitSet[] GetEan13Parity(int leadingDigit)
    {
        ValidateDigit(leadingDigit);
        return ConvertParity(Ean13ParityPatterns[leadingDigit]);
    }

    internal static EanUpcDigitSet[] GetUpcEParity(int numberSystem, int checkDigit)
    {
        if (numberSystem != 0 && numberSystem != 1)
        {
            throw new BarcodeValidationException("UPC-E number system must be zero or one.");
        }

        ValidateDigit(checkDigit);
        if (numberSystem == 0)
        {
            return ConvertParity(UpcENumberSystemZeroParityPatterns[checkDigit]);
        }

        return ConvertParity(UpcENumberSystemOneParityPatterns[checkDigit]);
    }

    internal static EanUpcDigitSet[] GetAddon2Parity(int remainder)
    {
        if (remainder < 0 || remainder >= Addon2ParityPatterns.Length)
        {
            throw new BarcodeValidationException("Two-digit add-on parity remainder must be zero through three.");
        }

        return ConvertParity(Addon2ParityPatterns[remainder]);
    }

    internal static EanUpcDigitSet[] GetAddon5Parity(int checksum)
    {
        ValidateDigit(checksum);
        return ConvertParity(Addon5ParityPatterns[checksum]);
    }

    static string SelectDigitPattern(int digit, EanUpcDigitSet digitSet)
    {
        switch (digitSet)
        {
            case EanUpcDigitSet.L:
                return LDigitModules[digit];
            case EanUpcDigitSet.G:
                return GDigitModules[digit];
            case EanUpcDigitSet.R:
                return RDigitModules[digit];
            default:
                throw new BarcodeValidationException("EAN/UPC digit set is invalid.");
        }
    }

    static byte[] ConvertModules(string pattern)
    {
        byte[] modules = new byte[pattern.Length];
        int index = 0;
        while (index < pattern.Length)
        {
            modules[index] = pattern[index] == '0' ? (byte)0 : (byte)1;
            index = index + 1;
        }

        return modules;
    }

    static EanUpcDigitSet[] ConvertParity(string pattern)
    {
        EanUpcDigitSet[] parity = new EanUpcDigitSet[pattern.Length];
        int index = 0;
        while (index < pattern.Length)
        {
            if (pattern[index] == 'L')
            {
                parity[index] = EanUpcDigitSet.L;
            }
            else
            {
                parity[index] = EanUpcDigitSet.G;
            }

            index = index + 1;
        }

        return parity;
    }

    static void ValidateDigit(int digit)
    {
        if (digit < 0 || digit >= DigitCount)
        {
            throw new BarcodeValidationException("EAN/UPC digit must be zero through nine.");
        }
    }
}
