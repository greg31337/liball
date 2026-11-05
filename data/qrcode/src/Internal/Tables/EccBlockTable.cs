namespace qrcode;

internal class EccBlockTable
{
    internal const int Low = 0;
    internal const int Medium = 1;
    internal const int Quartile = 2;
    internal const int High = 3;

    static readonly int[][] EccCodewordsPerBlock = new int[][]
    {
        new int[] { 7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
        new int[] { 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28 },
        new int[] { 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
        new int[] { 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 }
    };

    static readonly int[][] BlockCounts = new int[][]
    {
        new int[] { 1, 1, 1, 1, 1, 2, 2, 2, 2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25 },
        new int[] { 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49 },
        new int[] { 1, 1, 2, 2, 4, 4, 6, 6, 8, 8, 8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68 },
        new int[] { 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81 }
    };

    internal static int GetEccCodewordCountPerBlock(int version, int errorCorrectionIndex)
    {
        ValidateInputs(version, errorCorrectionIndex);
        return EccCodewordsPerBlock[errorCorrectionIndex][version - 1];
    }

    internal static int GetBlockCount(int version, int errorCorrectionIndex)
    {
        ValidateInputs(version, errorCorrectionIndex);
        return BlockCounts[errorCorrectionIndex][version - 1];
    }

    internal static int GetDataCodewordCount(int version, int errorCorrectionIndex)
    {
        int blockCount = GetBlockCount(version, errorCorrectionIndex);
        int eccCount = GetEccCodewordCountPerBlock(version, errorCorrectionIndex);
        return Model2VersionTable.GetTotalCodewordCount(version) - blockCount * eccCount;
    }

    internal static int GetShortBlockCount(int version, int errorCorrectionIndex)
    {
        int totalCodewords = Model2VersionTable.GetTotalCodewordCount(version);
        int blockCount = GetBlockCount(version, errorCorrectionIndex);
        return blockCount - totalCodewords % blockCount;
    }

    internal static int GetLongBlockCount(int version, int errorCorrectionIndex)
    {
        int totalCodewords = Model2VersionTable.GetTotalCodewordCount(version);
        int blockCount = GetBlockCount(version, errorCorrectionIndex);
        return totalCodewords % blockCount;
    }

    internal static int GetShortDataCodewordCount(int version, int errorCorrectionIndex)
    {
        int totalCodewords = Model2VersionTable.GetTotalCodewordCount(version);
        int blockCount = GetBlockCount(version, errorCorrectionIndex);
        return totalCodewords / blockCount - GetEccCodewordCountPerBlock(version, errorCorrectionIndex);
    }

    internal static int GetLongDataCodewordCount(int version, int errorCorrectionIndex)
    {
        if (GetLongBlockCount(version, errorCorrectionIndex) == 0)
        {
            return 0;
        }

        return GetShortDataCodewordCount(version, errorCorrectionIndex) + 1;
    }

    static void ValidateInputs(int version, int errorCorrectionIndex)
    {
        Model2VersionTable.ValidateVersion(version);
        if (errorCorrectionIndex < Low || errorCorrectionIndex > High)
        {
            throw new global::System.ArgumentOutOfRangeException("errorCorrectionIndex");
        }
    }
}
