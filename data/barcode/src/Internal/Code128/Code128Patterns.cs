namespace barcode;

internal enum Code128CodewordRole
{
    Data = 0,
    Function1 = 1,
    Function2 = 2,
    Function3 = 3,
    Function4 = 4,
    Shift = 5,
    CodeA = 6,
    CodeB = 7,
    CodeC = 8,
    StartA = 9,
    StartB = 10,
    StartC = 11,
    Stop = 12
}

internal static class Code128Patterns
{
    internal const int SymbolCharacterCount = 107;
    internal const int OrdinaryElementCount = 6;
    internal const int StopElementCount = 7;
    internal const int OrdinaryModuleCount = 11;
    internal const int StopModuleCount = 13;
    internal const int Function3Codeword = 96;
    internal const int Function2Codeword = 97;
    internal const int ShiftCodeword = 98;
    internal const int CodeCCodeword = 99;
    internal const int CodeBCodeword = 100;
    internal const int CodeACodeword = 101;
    internal const int Function4InCodeSetACodeword = 101;
    internal const int Function4InCodeSetBCodeword = 100;
    internal const int Function1Codeword = 102;
    internal const int StartACodeword = 103;
    internal const int StartBCodeword = 104;
    internal const int StartCCodeword = 105;
    internal const int StopCodeword = 106;

    static readonly string[] ElementWidthPatterns = new string[]
    {
        "212222",
        "222122",
        "222221",
        "121223",
        "121322",
        "131222",
        "122213",
        "122312",
        "132212",
        "221213",
        "221312",
        "231212",
        "112232",
        "122132",
        "122231",
        "113222",
        "123122",
        "123221",
        "223211",
        "221132",
        "221231",
        "213212",
        "223112",
        "312131",
        "311222",
        "321122",
        "321221",
        "312212",
        "322112",
        "322211",
        "212123",
        "212321",
        "232121",
        "111323",
        "131123",
        "131321",
        "112313",
        "132113",
        "132311",
        "211313",
        "231113",
        "231311",
        "112133",
        "112331",
        "132131",
        "113123",
        "113321",
        "133121",
        "313121",
        "211331",
        "231131",
        "213113",
        "213311",
        "213131",
        "311123",
        "311321",
        "331121",
        "312113",
        "312311",
        "332111",
        "314111",
        "221411",
        "431111",
        "111224",
        "111422",
        "121124",
        "121421",
        "141122",
        "141221",
        "112214",
        "112412",
        "122114",
        "122411",
        "142112",
        "142211",
        "241211",
        "221114",
        "413111",
        "241112",
        "134111",
        "111242",
        "121142",
        "121241",
        "114212",
        "124112",
        "124211",
        "411212",
        "421112",
        "421211",
        "212141",
        "214121",
        "412121",
        "111143",
        "111341",
        "131141",
        "114113",
        "114311",
        "411113",
        "411311",
        "113141",
        "114131",
        "311141",
        "411131",
        "211412",
        "211214",
        "211232",
        "2331112"
    };

    internal static int[] GetPatternWidths(int codeword)
    {
        ValidateCodeword(codeword);
        string pattern = ElementWidthPatterns[codeword];
        int[] widths = new int[pattern.Length];
        int index = 0;
        while (index < pattern.Length)
        {
            widths[index] = pattern[index] - '0';
            index = index + 1;
        }

        return widths;
    }

    internal static int GetElementCount(int codeword)
    {
        ValidateCodeword(codeword);
        if (codeword == StopCodeword)
        {
            return StopElementCount;
        }

        return OrdinaryElementCount;
    }

    internal static int GetModuleCount(int codeword)
    {
        ValidateCodeword(codeword);
        if (codeword == StopCodeword)
        {
            return StopModuleCount;
        }

        return OrdinaryModuleCount;
    }

    internal static Code128CodewordRole GetCodewordRole(int codeword, Code128CodeSet codeSet)
    {
        ValidateCodeword(codeword);
        switch (codeword)
        {
            case StartACodeword:
                return Code128CodewordRole.StartA;
            case StartBCodeword:
                return Code128CodewordRole.StartB;
            case StartCCodeword:
                return Code128CodewordRole.StartC;
            case StopCodeword:
                return Code128CodewordRole.Stop;
        }

        switch (codeSet)
        {
            case Code128CodeSet.A:
                return GetSetACodewordRole(codeword);
            case Code128CodeSet.B:
                return GetSetBCodewordRole(codeword);
            case Code128CodeSet.C:
                return GetSetCCodewordRole(codeword);
            default:
                throw new BarcodeValidationException("An active Code 128 code set is required to interpret a codeword.");
        }
    }

    static Code128CodewordRole GetSetACodewordRole(int codeword)
    {
        if (codeword <= 95)
        {
            return Code128CodewordRole.Data;
        }

        switch (codeword)
        {
            case Function3Codeword:
                return Code128CodewordRole.Function3;
            case Function2Codeword:
                return Code128CodewordRole.Function2;
            case ShiftCodeword:
                return Code128CodewordRole.Shift;
            case CodeCCodeword:
                return Code128CodewordRole.CodeC;
            case CodeBCodeword:
                return Code128CodewordRole.CodeB;
            case CodeACodeword:
                return Code128CodewordRole.Function4;
            case Function1Codeword:
                return Code128CodewordRole.Function1;
            default:
                throw new BarcodeValidationException("The codeword cannot be interpreted in Code 128 code set A.");
        }
    }

    static Code128CodewordRole GetSetBCodewordRole(int codeword)
    {
        if (codeword <= 95)
        {
            return Code128CodewordRole.Data;
        }

        switch (codeword)
        {
            case Function3Codeword:
                return Code128CodewordRole.Function3;
            case Function2Codeword:
                return Code128CodewordRole.Function2;
            case ShiftCodeword:
                return Code128CodewordRole.Shift;
            case CodeCCodeword:
                return Code128CodewordRole.CodeC;
            case CodeBCodeword:
                return Code128CodewordRole.Function4;
            case CodeACodeword:
                return Code128CodewordRole.CodeA;
            case Function1Codeword:
                return Code128CodewordRole.Function1;
            default:
                throw new BarcodeValidationException("The codeword cannot be interpreted in Code 128 code set B.");
        }
    }

    static Code128CodewordRole GetSetCCodewordRole(int codeword)
    {
        if (codeword <= 99)
        {
            return Code128CodewordRole.Data;
        }

        switch (codeword)
        {
            case CodeBCodeword:
                return Code128CodewordRole.CodeB;
            case CodeACodeword:
                return Code128CodewordRole.CodeA;
            case Function1Codeword:
                return Code128CodewordRole.Function1;
            default:
                throw new BarcodeValidationException("The codeword cannot be interpreted in Code 128 code set C.");
        }
    }

    internal static int GetStartCodeword(Code128CodeSet codeSet)
    {
        switch (codeSet)
        {
            case Code128CodeSet.A:
                return StartACodeword;
            case Code128CodeSet.B:
                return StartBCodeword;
            case Code128CodeSet.C:
                return StartCCodeword;
            default:
                throw new BarcodeValidationException("Automatic Code 128 selection does not identify a start codeword.");
        }
    }

    internal static Code128CodeSet GetCodeSetForStart(int codeword)
    {
        switch (codeword)
        {
            case StartACodeword:
                return Code128CodeSet.A;
            case StartBCodeword:
                return Code128CodeSet.B;
            case StartCCodeword:
                return Code128CodeSet.C;
            default:
                throw new BarcodeValidationException("The supplied codeword is not a Code 128 start character.");
        }
    }

    internal static void ValidateCodeword(int codeword)
    {
        if (codeword < 0 || codeword >= SymbolCharacterCount)
        {
            throw new BarcodeValidationException("Code 128 codeword must be from zero through 106.");
        }
    }
}
