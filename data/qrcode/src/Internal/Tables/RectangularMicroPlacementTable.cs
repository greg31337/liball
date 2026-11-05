namespace qrcode;

internal class RectangularMicroPlacementTable
{
    internal const int FinderPatternWidth = 7;
    internal const int SeparatorWidth = 1;
    internal const int AlignmentPatternWidth = 3;
    internal const int SubfinderPatternWidth = 5;
    internal const int CornerPatternWidth = 3;
    internal const int QuietZoneWidth = 2;

    internal static int GetFinderCenterRow(int versionIndex)
    {
        RectangularMicroVersionTable.ValidateVersionIndex(versionIndex);
        return 3;
    }

    internal static int GetFinderCenterColumn(int versionIndex)
    {
        RectangularMicroVersionTable.ValidateVersionIndex(versionIndex);
        return 3;
    }

    internal static int GetSubfinderCenterRow(int versionIndex)
    {
        return RectangularMicroVersionTable.GetHeight(versionIndex) - 3;
    }

    internal static int GetSubfinderCenterColumn(int versionIndex)
    {
        return RectangularMicroVersionTable.GetWidth(versionIndex) - 3;
    }

    internal static bool HasBottomSeparator(int versionIndex)
    {
        return RectangularMicroVersionTable.GetHeight(versionIndex) > 7;
    }

    internal static bool HasLowerLeftCornerPattern(int versionIndex)
    {
        return RectangularMicroVersionTable.GetHeight(versionIndex) > 7;
    }

    internal static bool HasLeftVerticalTimingPattern(int versionIndex)
    {
        return RectangularMicroVersionTable.GetHeight(versionIndex) >= 13;
    }

    internal static bool HasRightVerticalTimingPattern(int versionIndex)
    {
        return RectangularMicroVersionTable.GetHeight(versionIndex) >= 11;
    }

    internal static bool GetHorizontalTimingModuleValue(int versionIndex, int column)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        if (column < 0 || column >= width)
        {
            throw new global::System.ArgumentOutOfRangeException("column");
        }

        return column % 2 == 0;
    }

    internal static bool GetVerticalTimingModuleValue(int versionIndex, int row)
    {
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        if (row < 0 || row >= height)
        {
            throw new global::System.ArgumentOutOfRangeException("row");
        }

        return row % 2 == 0;
    }

    internal static int GetFinderSideFormatRow(int bitIndex)
    {
        ValidateFormatBitIndex(bitIndex);
        if (bitIndex < 15)
        {
            return bitIndex % 5 + 1;
        }

        return bitIndex - 14;
    }

    internal static int GetFinderSideFormatColumn(int bitIndex)
    {
        ValidateFormatBitIndex(bitIndex);
        if (bitIndex < 15)
        {
            return bitIndex / 5 + 8;
        }

        return 11;
    }

    internal static int GetSubfinderSideFormatRow(int versionIndex, int bitIndex)
    {
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        ValidateFormatBitIndex(bitIndex);
        if (bitIndex < 15)
        {
            return height - 6 + bitIndex % 5;
        }

        return height - 6;
    }

    internal static int GetSubfinderSideFormatColumn(int versionIndex, int bitIndex)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        ValidateFormatBitIndex(bitIndex);
        if (bitIndex < 15)
        {
            return width - 8 + bitIndex / 5;
        }

        return width + bitIndex - 20;
    }

    internal static int GetDataColumnPairCount(int versionIndex)
    {
        return (RectangularMicroVersionTable.GetWidth(versionIndex) - 1) / 2;
    }

    internal static int GetDataColumnPairRight(int versionIndex, int pairIndex)
    {
        int pairCount = GetDataColumnPairCount(versionIndex);
        if (pairIndex < 0 || pairIndex >= pairCount)
        {
            throw new global::System.ArgumentOutOfRangeException("pairIndex");
        }

        return RectangularMicroVersionTable.GetWidth(versionIndex) - 2 - pairIndex * 2;
    }

    internal static bool IsDataColumnPairUpward(int versionIndex, int pairIndex)
    {
        GetDataColumnPairRight(versionIndex, pairIndex);
        return pairIndex % 2 == 0;
    }

    static void ValidateFormatBitIndex(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex > 17)
        {
            throw new global::System.ArgumentOutOfRangeException("bitIndex");
        }
    }
}
