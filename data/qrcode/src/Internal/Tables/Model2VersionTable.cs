namespace qrcode;

internal class Model2VersionTable
{
    internal const int MinimumVersion = 1;
    internal const int MaximumVersion = 40;

    static readonly int[] TotalCodewords = new int[]
    {
        26, 44, 70, 100, 134, 172, 196, 242, 292, 346,
        404, 466, 532, 581, 655, 733, 815, 901, 991, 1085,
        1156, 1258, 1364, 1474, 1588, 1706, 1828, 1921, 2051, 2185,
        2323, 2465, 2611, 2761, 2876, 3034, 3196, 3362, 3532, 3706
    };

    static readonly int[] RemainderBits = new int[]
    {
        0, 7, 7, 7, 7, 7, 0, 0, 0, 0,
        0, 0, 0, 3, 3, 3, 3, 3, 3, 3,
        4, 4, 4, 4, 4, 4, 4, 3, 3, 3,
        3, 3, 3, 3, 0, 0, 0, 0, 0, 0
    };

    static readonly int[] CharacterCountWidths = new int[]
    {
        10, 12, 14,
        9, 11, 13,
        8, 16, 16,
        8, 10, 12
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

    internal static int GetRemainderBitCount(int version)
    {
        ValidateVersion(version);
        return RemainderBits[version - 1];
    }

    internal static int GetRawDataModuleCount(int version)
    {
        return GetTotalCodewordCount(version) * 8 + GetRemainderBitCount(version);
    }

    internal static int GetCharacterCountBitWidth(int modeIndex, int version)
    {
        ValidateVersion(version);
        if (modeIndex < 0 || modeIndex > 3)
        {
            throw new global::System.ArgumentOutOfRangeException("modeIndex");
        }

        int versionGroup = 0;
        if (version >= 27)
        {
            versionGroup = 2;
        }
        else if (version >= 10)
        {
            versionGroup = 1;
        }

        return CharacterCountWidths[modeIndex * 3 + versionGroup];
    }

    internal static void ValidateVersion(int version)
    {
        if (version < MinimumVersion || version > MaximumVersion)
        {
            throw new global::System.ArgumentOutOfRangeException("version");
        }
    }
}
