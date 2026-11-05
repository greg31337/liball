namespace qrcode;

internal class VersionInformationCodec
{
    internal static int CreateCode(int version)
    {
        VersionCodeTable.GetCode(version);
        return BchCodec.CreateCode(version, 6, 12, 0x1F25, 0);
    }

    internal static BchCorrectionResult CorrectCopies(int firstCode, int secondCode)
    {
        int[] codes = new int[34];
        int[] data = new int[34];
        for (int version = 7; version <= Model2VersionTable.MaximumVersion; version++)
        {
            int index = version - 7;
            codes[index] = CreateCode(version);
            data[index] = version;
        }

        return BchCodec.CorrectRedundantCodes(
            firstCode,
            secondCode,
            18,
            codes,
            codes,
            data,
            3);
    }
}
