namespace qrcode;

internal class LegacyModel1Table
{
    internal const int MinimumVersion = 1;
    internal const int MaximumVersion = 14;
    internal const int NumericMode = 0;
    internal const int AlphanumericMode = 1;
    internal const int ByteMode = 2;
    internal const int KanjiMode = 3;
    internal const int FormatInformationXorMask = 0x2825;
    internal const int FormatInformationModuleCount = 31;
    internal const int FirstSymbolCharacterBitCount = 4;
    internal const int SymbolCharacterBitCount = 8;
    internal const int LeadingVerticalSymbolColumnCount = 2;
    internal const int TrailingVerticalSymbolColumnCount = 4;
    internal const int QuietZoneWidth = 4;
    internal const bool HasAlignmentPatterns = false;
    internal const bool HasVersionInformation = false;
    internal const bool SupportsEci = false;
    internal const bool SupportsMirrorImaging = false;
    internal const bool SupportsReflectanceReversal = false;
    internal const bool InterleavesErrorCorrectionBlocks = false;

    static readonly int[] TotalCodewords = new int[]
    {
        26, 46, 72, 100, 134, 170, 212,
        256, 306, 358, 416, 476, 542, 610
    };

    static readonly int[] FunctionPatternModuleCounts = new int[]
    {
        206, 230, 238, 262, 270, 294, 302,
        326, 334, 358, 366, 390, 398, 422
    };

    static readonly int[][] DataCodewords = new int[][]
    {
        new int[] { 19, 36, 57, 80, 108, 136, 170, 208, 246, 290, 336, 384, 432, 489 },
        new int[] { 16, 30, 44, 60, 82, 106, 132, 160, 186, 222, 256, 292, 332, 368 },
        new int[] { 13, 24, 36, 50, 68, 86, 108, 128, 156, 183, 208, 244, 276, 310 },
        new int[] { 9, 16, 24, 34, 46, 58, 72, 87, 102, 124, 145, 165, 192, 210 }
    };

    static readonly int[][] ErrorCorrectionBlockCounts = new int[][]
    {
        new int[] { 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3 },
        new int[] { 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 4, 4, 4, 4 },
        new int[] { 1, 1, 1, 1, 1, 2, 2, 2, 3, 3, 4, 4, 4, 5 },
        new int[] { 1, 1, 1, 1, 2, 2, 3, 3, 3, 4, 5, 5, 6, 6 }
    };

    static readonly int[][] RemainderCodewordCounts = new int[][]
    {
        new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1 },
        new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2 },
        new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 2, 0 },
        new int[] { 0, 0, 0, 0, 0, 0, 2, 1, 0, 2, 1, 1, 2, 4 }
    };

    static readonly int[] CharacterCountBitWidths = new int[]
    {
        10, 12,
        9, 11,
        8, 16,
        8, 10
    };

    internal static int GetDimension(int version)
    {
        ValidateVersion(version);
        return 17 + version * 4;
    }

    internal static int GetTotalCodewordCount(int version)
    {
        ValidateVersion(version);
        return TotalCodewords[version - 1];
    }

    internal static int GetRawEncodingModuleCount(int version)
    {
        return GetTotalCodewordCount(version) * 8 - 4;
    }

    internal static int GetFunctionPatternModuleCount(int version)
    {
        ValidateVersion(version);
        return FunctionPatternModuleCounts[version - 1];
    }

    internal static int GetDataCodewordCount(int version, int errorCorrectionIndex)
    {
        ValidateInputs(version, errorCorrectionIndex);
        return DataCodewords[errorCorrectionIndex][version - 1];
    }

    internal static int GetDataBitCount(int version, int errorCorrectionIndex)
    {
        return GetDataCodewordCount(version, errorCorrectionIndex) * 8 - 4;
    }

    internal static int GetErrorCorrectionBlockCount(int version, int errorCorrectionIndex)
    {
        ValidateInputs(version, errorCorrectionIndex);
        return ErrorCorrectionBlockCounts[errorCorrectionIndex][version - 1];
    }

    internal static int GetRemainderCodewordCount(int version, int errorCorrectionIndex)
    {
        ValidateInputs(version, errorCorrectionIndex);
        return RemainderCodewordCounts[errorCorrectionIndex][version - 1];
    }

    internal static int GetErrorCorrectionCodewordCount(int version, int errorCorrectionIndex)
    {
        return GetTotalCodewordCount(version)
            - GetDataCodewordCount(version, errorCorrectionIndex)
            - GetRemainderCodewordCount(version, errorCorrectionIndex);
    }

    internal static int GetDataCodewordCountPerBlock(int version, int errorCorrectionIndex)
    {
        return GetDataCodewordCount(version, errorCorrectionIndex)
            / GetErrorCorrectionBlockCount(version, errorCorrectionIndex);
    }

    internal static int GetErrorCorrectionCodewordCountPerBlock(int version, int errorCorrectionIndex)
    {
        return GetErrorCorrectionCodewordCount(version, errorCorrectionIndex)
            / GetErrorCorrectionBlockCount(version, errorCorrectionIndex);
    }

    internal static int GetDataBlockStart(int version, int errorCorrectionIndex, int blockIndex)
    {
        ValidateBlockIndex(version, errorCorrectionIndex, blockIndex);
        return blockIndex * GetDataCodewordCountPerBlock(version, errorCorrectionIndex);
    }

    internal static int GetErrorCorrectionBlockStart(int version, int errorCorrectionIndex, int blockIndex)
    {
        ValidateBlockIndex(version, errorCorrectionIndex, blockIndex);
        return GetDataCodewordCount(version, errorCorrectionIndex)
            + blockIndex * GetErrorCorrectionCodewordCountPerBlock(version, errorCorrectionIndex);
    }

    internal static int GetCharacterCountBitWidth(int version, int modeIndex)
    {
        ValidateVersion(version);
        ValidateMode(modeIndex);
        int versionGroup = 0;
        if (version >= 10)
        {
            versionGroup = 1;
        }

        return CharacterCountBitWidths[modeIndex * 2 + versionGroup];
    }

    internal static int GetFormatCode(int errorCorrectionIndex, int mask)
    {
        return FormatCodeTable.GetCode(errorCorrectionIndex, mask)
            ^ 0x5412 ^ FormatInformationXorMask;
    }

    internal static bool IsExactFormatCode(int code)
    {
        if (code < 0 || code > 0x7FFF)
        {
            return false;
        }

        for (int errorCorrectionIndex = EccBlockTable.Low; errorCorrectionIndex <= EccBlockTable.High; errorCorrectionIndex++)
        {
            for (int mask = 0; mask < 8; mask++)
            {
                if (GetFormatCode(errorCorrectionIndex, mask) == code)
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static int GetExtensionBlockCountPerSide(int version)
    {
        ValidateVersion(version);
        return version / 2;
    }

    internal static int GetExtensionBlockStart(int version, int blockIndex)
    {
        int blockCount = GetExtensionBlockCountPerSide(version);
        if (blockIndex < 0 || blockIndex >= blockCount)
        {
            throw new global::System.ArgumentOutOfRangeException("blockIndex");
        }

        int first = 17;
        if ((version & 1) == 0)
        {
            first = 13;
        }

        return first + blockIndex * 8;
    }

    internal static bool IsExtensionModule(int version, int row, int column)
    {
        int dimension = GetDimension(version);
        ValidateCoordinate(row, dimension, "row");
        ValidateCoordinate(column, dimension, "column");
        if (row >= dimension - 2 && column >= dimension - 2)
        {
            return true;
        }

        int blockCount = GetExtensionBlockCountPerSide(version);
        for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
        {
            int start = GetExtensionBlockStart(version, blockIndex);
            bool inSpan = row >= start && row < start + 4;
            if (column >= dimension - 2 && inSpan)
            {
                return true;
            }

            inSpan = column >= start && column < start + 4;
            if (row >= dimension - 2 && inSpan)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsStandardExtensionDarkModule(int version, int row, int column)
    {
        int dimension = GetDimension(version);
        if (!IsExtensionModule(version, row, column))
        {
            return false;
        }

        if (row >= dimension - 2 && column >= dimension - 2)
        {
            return row == dimension - 1 && column == dimension - 1;
        }

        return row == dimension - 1 || column == dimension - 1;
    }

    internal static bool IsEarlyExtensionAdditionalDarkModule(int version, int row, int column)
    {
        int dimension = GetDimension(version);
        if (!IsExtensionModule(version, row, column))
        {
            return false;
        }

        int blockCount = GetExtensionBlockCountPerSide(version);
        for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
        {
            int start = GetExtensionBlockStart(version, blockIndex);
            if (row == dimension - 2 && (column == start || column == start + 3))
            {
                return true;
            }

            if (column == dimension - 2 && (row == start || row == start + 3))
            {
                return true;
            }
        }

        return false;
    }

    internal static int GetFormatPrimaryRow(int bitIndex)
    {
        return Model2PlacementTable.GetFormatPrimaryRow(bitIndex);
    }

    internal static int GetFormatPrimaryColumn(int bitIndex)
    {
        return Model2PlacementTable.GetFormatPrimaryColumn(bitIndex);
    }

    internal static int GetFormatSecondaryRow(int version, int bitIndex)
    {
        ValidateVersion(version);
        return Model2PlacementTable.GetFormatSecondaryRow(version, bitIndex);
    }

    internal static int GetFormatSecondaryColumn(int version, int bitIndex)
    {
        ValidateVersion(version);
        return Model2PlacementTable.GetFormatSecondaryColumn(version, bitIndex);
    }

    internal static int GetDarkModuleRow(int version)
    {
        return GetDimension(version) - 8;
    }

    internal static int GetDarkModuleColumn(int version)
    {
        ValidateVersion(version);
        return 8;
    }

    internal static bool IsVerticalSymbolCharacterColumn(int columnIndexFromRight, int columnCount)
    {
        if (columnCount < 1)
        {
            throw new global::System.ArgumentOutOfRangeException("columnCount");
        }

        if (columnIndexFromRight < 0 || columnIndexFromRight >= columnCount)
        {
            throw new global::System.ArgumentOutOfRangeException("columnIndexFromRight");
        }

        return columnIndexFromRight < LeadingVerticalSymbolColumnCount
            || columnIndexFromRight >= columnCount - TrailingVerticalSymbolColumnCount;
    }

    internal static int GetSymbolCharacterBitRow(int bitIndex, bool vertical, bool firstSymbolCharacter)
    {
        ValidateSymbolCharacterBitIndex(bitIndex, firstSymbolCharacter);
        if (firstSymbolCharacter)
        {
            return bitIndex / 2;
        }

        if (vertical)
        {
            return bitIndex / 2;
        }

        return bitIndex / 4;
    }

    internal static int GetSymbolCharacterBitColumn(int bitIndex, bool vertical, bool firstSymbolCharacter)
    {
        ValidateSymbolCharacterBitIndex(bitIndex, firstSymbolCharacter);
        if (firstSymbolCharacter || vertical)
        {
            return bitIndex % 2;
        }

        return bitIndex % 4;
    }

    internal static void ValidateVersion(int version)
    {
        if (version < MinimumVersion || version > MaximumVersion)
        {
            throw new global::System.ArgumentOutOfRangeException("version");
        }
    }

    static void ValidateInputs(int version, int errorCorrectionIndex)
    {
        ValidateVersion(version);
        if (errorCorrectionIndex < EccBlockTable.Low || errorCorrectionIndex > EccBlockTable.High)
        {
            throw new global::System.ArgumentOutOfRangeException("errorCorrectionIndex");
        }
    }

    static void ValidateMode(int modeIndex)
    {
        if (modeIndex < NumericMode || modeIndex > KanjiMode)
        {
            throw new global::System.ArgumentOutOfRangeException("modeIndex");
        }
    }

    static void ValidateBlockIndex(int version, int errorCorrectionIndex, int blockIndex)
    {
        int blockCount = GetErrorCorrectionBlockCount(version, errorCorrectionIndex);
        if (blockIndex < 0 || blockIndex >= blockCount)
        {
            throw new global::System.ArgumentOutOfRangeException("blockIndex");
        }
    }

    static void ValidateCoordinate(int coordinate, int dimension, string parameterName)
    {
        if (coordinate < 0 || coordinate >= dimension)
        {
            throw new global::System.ArgumentOutOfRangeException(parameterName);
        }
    }

    static void ValidateSymbolCharacterBitIndex(int bitIndex, bool firstSymbolCharacter)
    {
        int bitCount = SymbolCharacterBitCount;
        if (firstSymbolCharacter)
        {
            bitCount = FirstSymbolCharacterBitCount;
        }

        if (bitIndex < 0 || bitIndex >= bitCount)
        {
            throw new global::System.ArgumentOutOfRangeException("bitIndex");
        }
    }
}
