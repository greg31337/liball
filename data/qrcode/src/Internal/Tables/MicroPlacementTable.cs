namespace qrcode;

internal class MicroPlacementTable
{
    internal const int FinderPatternWidth = 7;
    internal const int SeparatorWidth = 1;
    internal const int TimingStartIndex = 8;
    internal const int FormatAxisIndex = 8;
    internal const int QuietZoneWidth = 2;

    internal static int GetFinderCenterRow(int version)
    {
        MicroVersionTable.ValidateVersion(version);
        return 3;
    }

    internal static int GetFinderCenterColumn(int version)
    {
        MicroVersionTable.ValidateVersion(version);
        return 3;
    }

    internal static bool GetTimingModuleValue(int version, int index)
    {
        int dimension = MicroVersionTable.GetDimension(version);
        if (index < TimingStartIndex || index >= dimension)
        {
            throw new global::System.ArgumentOutOfRangeException("index");
        }

        return index % 2 == 0;
    }

    internal static int GetFormatVerticalRow(int bitIndex)
    {
        ValidateVerticalFormatBitIndex(bitIndex);
        return bitIndex + 1;
    }

    internal static int GetFormatVerticalColumn(int bitIndex)
    {
        ValidateVerticalFormatBitIndex(bitIndex);
        return FormatAxisIndex;
    }

    internal static int GetFormatHorizontalRow(int bitIndex)
    {
        ValidateHorizontalFormatBitIndex(bitIndex);
        return FormatAxisIndex;
    }

    internal static int GetFormatHorizontalColumn(int bitIndex)
    {
        ValidateHorizontalFormatBitIndex(bitIndex);
        return 15 - bitIndex;
    }

    internal static int GetDataColumnPairCount(int version)
    {
        return (MicroVersionTable.GetDimension(version) - 1) / 2;
    }

    internal static int GetDataColumnPairRight(int version, int pairIndex)
    {
        int pairCount = GetDataColumnPairCount(version);
        if (pairIndex < 0 || pairIndex >= pairCount)
        {
            throw new global::System.ArgumentOutOfRangeException("pairIndex");
        }

        return MicroVersionTable.GetDimension(version) - 1 - pairIndex * 2;
    }

    internal static bool IsDataColumnPairUpward(int version, int pairIndex)
    {
        GetDataColumnPairRight(version, pairIndex);
        return pairIndex % 2 == 0;
    }

    static void ValidateVerticalFormatBitIndex(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex > 7)
        {
            throw new global::System.ArgumentOutOfRangeException("bitIndex");
        }
    }

    static void ValidateHorizontalFormatBitIndex(int bitIndex)
    {
        if (bitIndex < 7 || bitIndex > 14)
        {
            throw new global::System.ArgumentOutOfRangeException("bitIndex");
        }
    }
}
