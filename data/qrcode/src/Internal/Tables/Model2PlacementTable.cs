namespace qrcode;

internal class Model2PlacementTable
{
    internal const int FinderPatternWidth = 7;
    internal const int SeparatorWidth = 1;
    internal const int AlignmentPatternWidth = 5;
    internal const int TimingPatternIndex = 6;
    internal const int QuietZoneWidth = 4;

    internal static int GetFinderCenterRow(int version, int finderIndex)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        ValidateFinderIndex(finderIndex);
        if (finderIndex == 2)
        {
            return dimension - 4;
        }

        return 3;
    }

    internal static int GetFinderCenterColumn(int version, int finderIndex)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        ValidateFinderIndex(finderIndex);
        if (finderIndex == 1)
        {
            return dimension - 4;
        }

        return 3;
    }

    internal static int GetDataColumnPairCount(int version)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        return (dimension - 1) / 2;
    }

    internal static int GetDataColumnPairRight(int version, int pairIndex)
    {
        int pairCount = GetDataColumnPairCount(version);
        if (pairIndex < 0 || pairIndex >= pairCount)
        {
            throw new global::System.ArgumentOutOfRangeException("pairIndex");
        }

        int right = Model2VersionTable.GetDimension(version) - 1 - pairIndex * 2;
        if (right <= 6)
        {
            right--;
        }

        return right;
    }

    internal static bool IsDataColumnPairUpward(int version, int pairIndex)
    {
        int right = GetDataColumnPairRight(version, pairIndex);
        return ((right + 1) & 2) == 0;
    }

    internal static int GetFormatPrimaryRow(int bitIndex)
    {
        ValidateFormatBitIndex(bitIndex);
        if (bitIndex <= 7)
        {
            if (bitIndex == 6)
            {
                return 7;
            }

            if (bitIndex == 7)
            {
                return 8;
            }

            return bitIndex;
        }

        return 8;
    }

    internal static int GetFormatPrimaryColumn(int bitIndex)
    {
        ValidateFormatBitIndex(bitIndex);
        if (bitIndex <= 7)
        {
            return 8;
        }

        if (bitIndex == 8)
        {
            return 7;
        }

        return 14 - bitIndex;
    }

    internal static int GetFormatSecondaryRow(int version, int bitIndex)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        ValidateFormatBitIndex(bitIndex);
        if (bitIndex < 8)
        {
            return 8;
        }

        return dimension - 15 + bitIndex;
    }

    internal static int GetFormatSecondaryColumn(int version, int bitIndex)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        ValidateFormatBitIndex(bitIndex);
        if (bitIndex < 8)
        {
            return dimension - 1 - bitIndex;
        }

        return 8;
    }

    internal static int GetDarkModuleRow(int version)
    {
        return Model2VersionTable.GetDimension(version) - 8;
    }

    internal static int GetDarkModuleColumn(int version)
    {
        Model2VersionTable.ValidateVersion(version);
        return 8;
    }

    internal static int GetVersionPrimaryRow(int version, int bitIndex)
    {
        ValidateVersionBitIndex(version, bitIndex);
        return bitIndex / 3;
    }

    internal static int GetVersionPrimaryColumn(int version, int bitIndex)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        ValidateVersionBitIndex(version, bitIndex);
        return dimension - 11 + bitIndex % 3;
    }

    internal static int GetVersionSecondaryRow(int version, int bitIndex)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        ValidateVersionBitIndex(version, bitIndex);
        return dimension - 11 + bitIndex % 3;
    }

    internal static int GetVersionSecondaryColumn(int version, int bitIndex)
    {
        ValidateVersionBitIndex(version, bitIndex);
        return bitIndex / 3;
    }

    static void ValidateFormatBitIndex(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex > 14)
        {
            throw new global::System.ArgumentOutOfRangeException("bitIndex");
        }
    }

    static void ValidateFinderIndex(int finderIndex)
    {
        if (finderIndex < 0 || finderIndex > 2)
        {
            throw new global::System.ArgumentOutOfRangeException("finderIndex");
        }
    }

    static void ValidateVersionBitIndex(int version, int bitIndex)
    {
        if (version < 7 || version > Model2VersionTable.MaximumVersion)
        {
            throw new global::System.ArgumentOutOfRangeException("version");
        }

        if (bitIndex < 0 || bitIndex > 17)
        {
            throw new global::System.ArgumentOutOfRangeException("bitIndex");
        }
    }
}
