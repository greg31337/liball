namespace qrcode;

internal class FormatCodeTable
{
    static readonly int[] Codes = new int[]
    {
        0x77C4, 0x72F3, 0x7DAA, 0x789D, 0x662F, 0x6318, 0x6C41, 0x6976,
        0x5412, 0x5125, 0x5E7C, 0x5B4B, 0x45F9, 0x40CE, 0x4F97, 0x4AA0,
        0x355F, 0x3068, 0x3F31, 0x3A06, 0x24B4, 0x2183, 0x2EDA, 0x2BED,
        0x1689, 0x13BE, 0x1CE7, 0x19D0, 0x0762, 0x0255, 0x0D0C, 0x083B
    };

    internal static int GetCode(int errorCorrectionIndex, int mask)
    {
        if (errorCorrectionIndex < EccBlockTable.Low || errorCorrectionIndex > EccBlockTable.High)
        {
            throw new global::System.ArgumentOutOfRangeException("errorCorrectionIndex");
        }

        if (mask < 0 || mask > 7)
        {
            throw new global::System.ArgumentOutOfRangeException("mask");
        }

        return Codes[errorCorrectionIndex * 8 + mask];
    }
}
