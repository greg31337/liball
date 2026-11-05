namespace qrcode;

internal class MicroVersionTable
{
    internal const int MinimumVersion = 1;
    internal const int MaximumVersion = 4;
    internal const int DetectionOnly = -1;
    internal const int NumericMode = 0;
    internal const int AlphanumericMode = 1;
    internal const int ByteMode = 2;
    internal const int KanjiMode = 3;

    static readonly int[] Dimensions = new int[] { 11, 13, 15, 17 };
    static readonly int[] TotalEncodingBits = new int[] { 36, 80, 132, 192 };
    static readonly int[] SymbolCharacterCounts = new int[] { 5, 10, 17, 24 };
    static readonly int[] FinalDataSymbolBitWidths = new int[] { 4, 8, 4, 8 };
    static readonly int[] TerminatorBitCounts = new int[] { 3, 5, 7, 9 };
    static readonly int[] ModeIndicatorBitWidths = new int[] { 0, 1, 2, 3 };

    static readonly int[] CharacterCountBitWidths = new int[]
    {
        3, 4, 5, 6,
        0, 3, 4, 5,
        0, 0, 4, 5,
        0, 0, 3, 4
    };

    static readonly int[] DataBitCapacities = new int[]
    {
        20, 40, 32, 84, 68, 128, 112, 80
    };

    static readonly int[] DataSymbolCharacterCounts = new int[]
    {
        3, 5, 4, 11, 9, 16, 14, 10
    };

    static readonly int[] EccCodewordCounts = new int[]
    {
        2, 5, 6, 6, 8, 8, 10, 14
    };

    static readonly int[] MaximumCharacterCounts = new int[]
    {
        5, 0, 0, 0,
        10, 6, 0, 0,
        8, 5, 0, 0,
        23, 14, 9, 6,
        18, 11, 7, 4,
        35, 21, 15, 9,
        30, 18, 13, 8,
        21, 13, 9, 5
    };

    internal static int GetDimension(int version)
    {
        ValidateVersion(version);
        return Dimensions[version - 1];
    }

    internal static int GetQuietZoneWidth(int version)
    {
        ValidateVersion(version);
        return 2;
    }

    internal static int GetTotalEncodingBitCount(int version)
    {
        ValidateVersion(version);
        return TotalEncodingBits[version - 1];
    }

    internal static int GetSymbolCharacterCount(int version)
    {
        ValidateVersion(version);
        return SymbolCharacterCounts[version - 1];
    }

    internal static int GetFinalDataSymbolBitWidth(int version)
    {
        ValidateVersion(version);
        return FinalDataSymbolBitWidths[version - 1];
    }

    internal static int GetTerminatorBitCount(int version)
    {
        ValidateVersion(version);
        return TerminatorBitCounts[version - 1];
    }

    internal static int GetModeIndicatorBitWidth(int version)
    {
        ValidateVersion(version);
        return ModeIndicatorBitWidths[version - 1];
    }

    internal static bool IsModeSupported(int version, int modeIndex)
    {
        ValidateVersion(version);
        ValidateMode(modeIndex);
        return CharacterCountBitWidths[modeIndex * 4 + version - 1] != 0;
    }

    internal static int GetModeIndicator(int version, int modeIndex)
    {
        if (!IsModeSupported(version, modeIndex))
        {
            throw new global::System.ArgumentException("The mode is not supported by the Micro QR version.", "modeIndex");
        }

        return modeIndex;
    }

    internal static int GetCharacterCountBitWidth(int version, int modeIndex)
    {
        if (!IsModeSupported(version, modeIndex))
        {
            throw new global::System.ArgumentException("The mode is not supported by the Micro QR version.", "modeIndex");
        }

        return CharacterCountBitWidths[modeIndex * 4 + version - 1];
    }

    internal static bool IsErrorCorrectionSupported(int version, int errorCorrectionIndex)
    {
        ValidateVersion(version);
        ValidateErrorCorrection(errorCorrectionIndex);
        if (version == 1)
        {
            return errorCorrectionIndex == DetectionOnly;
        }

        if (version == 4)
        {
            return errorCorrectionIndex >= EccBlockTable.Low && errorCorrectionIndex <= EccBlockTable.Quartile;
        }

        return errorCorrectionIndex == EccBlockTable.Low || errorCorrectionIndex == EccBlockTable.Medium;
    }

    internal static int GetSymbolNumber(int version, int errorCorrectionIndex)
    {
        if (!IsErrorCorrectionSupported(version, errorCorrectionIndex))
        {
            throw new global::System.ArgumentException("The error correction level is not supported by the Micro QR version.", "errorCorrectionIndex");
        }

        if (version == 1)
        {
            return 0;
        }

        if (version == 2)
        {
            return 1 + errorCorrectionIndex;
        }

        if (version == 3)
        {
            return 3 + errorCorrectionIndex;
        }

        return 5 + errorCorrectionIndex;
    }

    internal static int GetDataBitCapacity(int version, int errorCorrectionIndex)
    {
        return DataBitCapacities[GetSymbolNumber(version, errorCorrectionIndex)];
    }

    internal static int GetDataSymbolCharacterCount(int version, int errorCorrectionIndex)
    {
        return DataSymbolCharacterCounts[GetSymbolNumber(version, errorCorrectionIndex)];
    }

    internal static int GetEccCodewordCount(int version, int errorCorrectionIndex)
    {
        return EccCodewordCounts[GetSymbolNumber(version, errorCorrectionIndex)];
    }

    internal static int GetErrorCorrectionBlockCount(int version, int errorCorrectionIndex)
    {
        GetSymbolNumber(version, errorCorrectionIndex);
        return 1;
    }

    internal static int GetMaximumCharacterCount(int version, int errorCorrectionIndex, int modeIndex)
    {
        if (!IsModeSupported(version, modeIndex))
        {
            throw new global::System.ArgumentException("The mode is not supported by the Micro QR version.", "modeIndex");
        }

        int symbolNumber = GetSymbolNumber(version, errorCorrectionIndex);
        return MaximumCharacterCounts[symbolNumber * 4 + modeIndex];
    }

    internal static void ValidateVersion(int version)
    {
        if (version < MinimumVersion || version > MaximumVersion)
        {
            throw new global::System.ArgumentOutOfRangeException("version");
        }
    }

    static void ValidateMode(int modeIndex)
    {
        if (modeIndex < NumericMode || modeIndex > KanjiMode)
        {
            throw new global::System.ArgumentOutOfRangeException("modeIndex");
        }
    }

    static void ValidateErrorCorrection(int errorCorrectionIndex)
    {
        if (errorCorrectionIndex < DetectionOnly || errorCorrectionIndex > EccBlockTable.Quartile)
        {
            throw new global::System.ArgumentOutOfRangeException("errorCorrectionIndex");
        }
    }
}
