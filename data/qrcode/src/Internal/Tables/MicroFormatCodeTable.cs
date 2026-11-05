namespace qrcode;

internal class MicroFormatCodeTable
{
    static readonly int[] Codes = new int[]
    {
        0x4445, 0x4172, 0x4E2B, 0x4B1C,
        0x55AE, 0x5099, 0x5FC0, 0x5AF7,
        0x6793, 0x62A4, 0x6DFD, 0x68CA,
        0x7678, 0x734F, 0x7C16, 0x7921,
        0x06DE, 0x03E9, 0x0CB0, 0x0987,
        0x1735, 0x1202, 0x1D5B, 0x186C,
        0x2508, 0x203F, 0x2F66, 0x2A51,
        0x34E3, 0x31D4, 0x3E8D, 0x3BBA
    };

    internal static int GetCode(int version, int errorCorrectionIndex, int mask)
    {
        if (mask < 0 || mask > 3)
        {
            throw new global::System.ArgumentOutOfRangeException("mask");
        }

        int symbolNumber = MicroVersionTable.GetSymbolNumber(version, errorCorrectionIndex);
        return Codes[symbolNumber * 4 + mask];
    }
}
