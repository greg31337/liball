namespace qrcode;

internal class VersionCodeTable
{
    static readonly int[] Codes = new int[]
    {
        0x07C94, 0x085BC, 0x09A99, 0x0A4D3, 0x0BBF6,
        0x0C762, 0x0D847, 0x0E60D, 0x0F928, 0x10B78,
        0x1145D, 0x12A17, 0x13532, 0x149A6, 0x15683,
        0x168C9, 0x177EC, 0x18EC4, 0x191E1, 0x1AFAB,
        0x1B08E, 0x1CC1A, 0x1D33F, 0x1ED75, 0x1F250,
        0x209D5, 0x216F0, 0x228BA, 0x2379F, 0x24B0B,
        0x2542E, 0x26A64, 0x27541, 0x28C69
    };

    internal static int GetCode(int version)
    {
        if (version < 7 || version > Model2VersionTable.MaximumVersion)
        {
            throw new global::System.ArgumentOutOfRangeException("version");
        }

        return Codes[version - 7];
    }
}
