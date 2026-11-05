namespace qrcode;

internal class RectangularMicroVersionTable
{
    internal const int MinimumVersionIndex = 0;
    internal const int MaximumVersionIndex = 31;
    internal const int Medium = 0;
    internal const int High = 1;
    internal const int NumericMode = 0;
    internal const int AlphanumericMode = 1;
    internal const int ByteMode = 2;
    internal const int KanjiMode = 3;
    internal const int ModeIndicatorBitWidth = 3;
    internal const int TerminatorBitCount = 3;
    internal const int TerminatorModeIndicator = 0;
    internal const int Fnc1FirstModeIndicator = 5;
    internal const int Fnc1SecondModeIndicator = 6;
    internal const int EciModeIndicator = 7;
    internal const int FormatInformationModuleCount = 36;
    internal const int QuietZoneWidth = 2;

    static readonly int[] Heights = new int[]
    {
        7, 7, 7, 7, 7,
        9, 9, 9, 9, 9,
        11, 11, 11, 11, 11, 11,
        13, 13, 13, 13, 13, 13,
        15, 15, 15, 15, 15,
        17, 17, 17, 17, 17
    };

    static readonly int[] Widths = new int[]
    {
        43, 59, 77, 99, 139,
        43, 59, 77, 99, 139,
        27, 43, 59, 77, 99, 139,
        27, 43, 59, 77, 99, 139,
        43, 59, 77, 99, 139,
        43, 59, 77, 99, 139
    };

    static readonly int[] TotalCodewords = new int[]
    {
        13, 21, 32, 44, 68,
        21, 33, 49, 66, 99,
        15, 31, 47, 67, 89, 132,
        21, 41, 60, 85, 113, 166,
        51, 74, 103, 136, 199,
        61, 88, 122, 160, 232
    };

    static readonly int[] RemainderBits = new int[]
    {
        0, 3, 5, 6, 1,
        2, 3, 1, 4, 5,
        2, 1, 0, 2, 7, 6,
        4, 1, 6, 4, 3, 0,
        1, 4, 6, 7, 2,
        1, 2, 0, 3, 4
    };

    static readonly int[][] CharacterCountBitWidths = new int[][]
    {
        new int[] { 4, 5, 6, 7, 7, 5, 6, 7, 7, 8, 4, 6, 7, 7, 8, 8, 5, 6, 7, 7, 8, 8, 7, 7, 8, 8, 9, 7, 8, 8, 8, 9 },
        new int[] { 3, 5, 5, 6, 6, 5, 5, 6, 6, 7, 4, 5, 6, 6, 7, 7, 5, 6, 6, 7, 7, 8, 6, 7, 7, 7, 8, 6, 7, 7, 8, 8 },
        new int[] { 3, 4, 5, 5, 6, 4, 5, 5, 6, 6, 3, 5, 5, 6, 6, 7, 4, 5, 6, 6, 7, 7, 6, 6, 7, 7, 7, 6, 6, 7, 7, 8 },
        new int[] { 2, 3, 4, 5, 5, 3, 4, 5, 5, 6, 2, 4, 5, 5, 6, 6, 3, 5, 5, 6, 6, 7, 5, 5, 6, 6, 7, 5, 6, 6, 6, 7 }
    };

    static readonly int[][] DataCodewords = new int[][]
    {
        new int[] { 6, 12, 20, 28, 44, 12, 21, 31, 42, 63, 7, 19, 31, 43, 57, 84, 12, 27, 38, 53, 73, 106, 33, 48, 67, 88, 127, 39, 56, 78, 100, 152 },
        new int[] { 3, 7, 10, 14, 24, 7, 11, 17, 22, 33, 5, 11, 15, 23, 29, 42, 7, 13, 20, 29, 35, 54, 15, 26, 31, 48, 69, 21, 28, 38, 56, 76 }
    };

    static readonly int[][] BlockCounts = new int[][]
    {
        new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 2, 2, 1, 1, 1, 2, 2, 3, 1, 1, 2, 2, 3, 1, 2, 2, 3, 4 },
        new int[] { 1, 1, 1, 1, 2, 1, 1, 2, 2, 3, 1, 1, 2, 2, 2, 3, 1, 1, 2, 2, 3, 4, 2, 2, 3, 4, 5, 2, 2, 3, 4, 6 }
    };

    static readonly int[][] MaximumCharacterCounts = new int[][]
    {
        new int[] { 12, 7, 5, 3, 5, 3, 2, 1 },
        new int[] { 26, 16, 11, 6, 14, 8, 6, 3 },
        new int[] { 45, 27, 19, 11, 21, 13, 9, 5 },
        new int[] { 64, 39, 27, 16, 30, 18, 13, 8 },
        new int[] { 102, 62, 42, 26, 54, 33, 22, 14 },
        new int[] { 26, 16, 11, 6, 14, 8, 6, 3 },
        new int[] { 47, 29, 20, 12, 23, 14, 10, 6 },
        new int[] { 71, 43, 30, 18, 37, 23, 16, 9 },
        new int[] { 97, 59, 40, 25, 49, 30, 20, 12 },
        new int[] { 147, 89, 61, 38, 75, 46, 31, 19 },
        new int[] { 14, 8, 6, 3, 9, 6, 4, 2 },
        new int[] { 42, 26, 18, 11, 23, 14, 10, 6 },
        new int[] { 71, 43, 30, 18, 33, 20, 14, 8 },
        new int[] { 100, 60, 41, 25, 52, 31, 21, 13 },
        new int[] { 133, 81, 55, 34, 66, 40, 27, 17 },
        new int[] { 198, 120, 82, 51, 97, 59, 40, 25 },
        new int[] { 26, 16, 11, 6, 14, 8, 6, 3 },
        new int[] { 62, 37, 26, 16, 28, 17, 12, 7 },
        new int[] { 88, 53, 36, 22, 45, 27, 18, 11 },
        new int[] { 124, 75, 51, 31, 66, 40, 27, 17 },
        new int[] { 171, 104, 71, 44, 80, 49, 33, 20 },
        new int[] { 251, 152, 104, 64, 126, 76, 52, 32 },
        new int[] { 76, 46, 31, 19, 33, 20, 13, 8 },
        new int[] { 112, 68, 46, 28, 59, 36, 24, 15 },
        new int[] { 157, 95, 65, 40, 71, 43, 29, 18 },
        new int[] { 207, 126, 86, 53, 111, 68, 46, 28 },
        new int[] { 301, 182, 125, 77, 162, 98, 67, 41 },
        new int[] { 90, 55, 37, 23, 47, 28, 19, 12 },
        new int[] { 131, 79, 54, 33, 63, 38, 26, 16 },
        new int[] { 183, 111, 76, 47, 87, 53, 36, 22 },
        new int[] { 236, 143, 98, 60, 131, 79, 54, 33 },
        new int[] { 361, 219, 150, 92, 178, 108, 74, 46 }
    };

    internal static int GetHeight(int versionIndex)
    {
        ValidateVersionIndex(versionIndex);
        return Heights[versionIndex];
    }

    internal static int GetWidth(int versionIndex)
    {
        ValidateVersionIndex(versionIndex);
        return Widths[versionIndex];
    }

    internal static string GetVersionName(int versionIndex)
    {
        return $"R{GetHeight(versionIndex)}x{GetWidth(versionIndex)}";
    }

    internal static int GetVersionIndex(int height, int width)
    {
        for (int versionIndex = MinimumVersionIndex; versionIndex <= MaximumVersionIndex; versionIndex++)
        {
            if (Heights[versionIndex] == height && Widths[versionIndex] == width)
            {
                return versionIndex;
            }
        }

        throw new global::System.ArgumentException("The dimensions do not identify an rMQR version.", "height");
    }

    internal static int GetTotalCodewordCount(int versionIndex)
    {
        ValidateVersionIndex(versionIndex);
        return TotalCodewords[versionIndex];
    }

    internal static int GetRemainderBitCount(int versionIndex)
    {
        ValidateVersionIndex(versionIndex);
        return RemainderBits[versionIndex];
    }

    internal static int GetFunctionPatternModuleCount(int versionIndex)
    {
        int area = GetHeight(versionIndex) * GetWidth(versionIndex);
        return area - FormatInformationModuleCount - GetTotalCodewordCount(versionIndex) * 8 - GetRemainderBitCount(versionIndex);
    }

    internal static int GetModeIndicator(int modeIndex)
    {
        ValidateMode(modeIndex);
        return modeIndex + 1;
    }

    internal static int GetCharacterCountBitWidth(int versionIndex, int modeIndex)
    {
        ValidateVersionIndex(versionIndex);
        ValidateMode(modeIndex);
        return CharacterCountBitWidths[modeIndex][versionIndex];
    }

    internal static int GetDataCodewordCount(int versionIndex, int errorCorrectionIndex)
    {
        ValidateInputs(versionIndex, errorCorrectionIndex);
        return DataCodewords[errorCorrectionIndex][versionIndex];
    }

    internal static int GetDataBitCapacity(int versionIndex, int errorCorrectionIndex)
    {
        return GetDataCodewordCount(versionIndex, errorCorrectionIndex) * 8;
    }

    internal static int GetEccCodewordCount(int versionIndex, int errorCorrectionIndex)
    {
        return GetTotalCodewordCount(versionIndex) - GetDataCodewordCount(versionIndex, errorCorrectionIndex);
    }

    internal static int GetBlockCount(int versionIndex, int errorCorrectionIndex)
    {
        ValidateInputs(versionIndex, errorCorrectionIndex);
        return BlockCounts[errorCorrectionIndex][versionIndex];
    }

    internal static int GetEccCodewordCountPerBlock(int versionIndex, int errorCorrectionIndex)
    {
        return GetEccCodewordCount(versionIndex, errorCorrectionIndex) / GetBlockCount(versionIndex, errorCorrectionIndex);
    }

    internal static int GetShortBlockCount(int versionIndex, int errorCorrectionIndex)
    {
        int dataCodewords = GetDataCodewordCount(versionIndex, errorCorrectionIndex);
        int blocks = GetBlockCount(versionIndex, errorCorrectionIndex);
        return blocks - dataCodewords % blocks;
    }

    internal static int GetLongBlockCount(int versionIndex, int errorCorrectionIndex)
    {
        int dataCodewords = GetDataCodewordCount(versionIndex, errorCorrectionIndex);
        int blocks = GetBlockCount(versionIndex, errorCorrectionIndex);
        return dataCodewords % blocks;
    }

    internal static int GetShortDataCodewordCount(int versionIndex, int errorCorrectionIndex)
    {
        return GetDataCodewordCount(versionIndex, errorCorrectionIndex) / GetBlockCount(versionIndex, errorCorrectionIndex);
    }

    internal static int GetLongDataCodewordCount(int versionIndex, int errorCorrectionIndex)
    {
        if (GetLongBlockCount(versionIndex, errorCorrectionIndex) == 0)
        {
            return 0;
        }

        return GetShortDataCodewordCount(versionIndex, errorCorrectionIndex) + 1;
    }

    internal static int GetMaximumCharacterCount(int versionIndex, int errorCorrectionIndex, int modeIndex)
    {
        ValidateInputs(versionIndex, errorCorrectionIndex);
        ValidateMode(modeIndex);
        return MaximumCharacterCounts[versionIndex][errorCorrectionIndex * 4 + modeIndex];
    }

    internal static void ValidateVersionIndex(int versionIndex)
    {
        if (versionIndex < MinimumVersionIndex || versionIndex > MaximumVersionIndex)
        {
            throw new global::System.ArgumentOutOfRangeException("versionIndex");
        }
    }

    static void ValidateMode(int modeIndex)
    {
        if (modeIndex < NumericMode || modeIndex > KanjiMode)
        {
            throw new global::System.ArgumentOutOfRangeException("modeIndex");
        }
    }

    static void ValidateInputs(int versionIndex, int errorCorrectionIndex)
    {
        ValidateVersionIndex(versionIndex);
        if (errorCorrectionIndex < Medium || errorCorrectionIndex > High)
        {
            throw new global::System.ArgumentOutOfRangeException("errorCorrectionIndex");
        }
    }
}
