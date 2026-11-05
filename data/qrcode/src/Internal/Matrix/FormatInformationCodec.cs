namespace qrcode;

internal class FormatInformationCodec
{
    internal static int CreateModel2Code(int errorCorrectionIndex, int mask)
    {
        int data = GetErrorCorrectionBits(errorCorrectionIndex) << 3 | mask;
        FormatCodeTable.GetCode(errorCorrectionIndex, mask);
        return BchCodec.CreateCode(data, 5, 10, 0x537, 0x5412);
    }

    internal static BchCorrectionResult CorrectModel2Copies(
        int firstCode,
        int secondCode)
    {
        int[] codes = new int[32];
        int[] data = new int[32];
        int index = 0;
        for (int level = 0; level < 4; level++)
        {
            for (int mask = 0; mask < 8; mask++)
            {
                codes[index] = CreateModel2Code(level, mask);
                data[index] = level * 8 + mask;
                index++;
            }
        }

        return BchCodec.CorrectRedundantCodes(
            firstCode,
            secondCode,
            15,
            codes,
            codes,
            data,
            3);
    }

    internal static int CreateMicroCode(
        int version,
        int errorCorrectionIndex,
        int mask)
    {
        int symbolNumber = MicroVersionTable.GetSymbolNumber(version, errorCorrectionIndex);
        MicroFormatCodeTable.GetCode(version, errorCorrectionIndex, mask);
        int data = symbolNumber << 2 | mask;
        return BchCodec.CreateCode(data, 5, 10, 0x537, 0x4445);
    }

    internal static BchCorrectionResult CorrectMicroCopies(int firstCode, int secondCode)
    {
        int[] codes = new int[32];
        int[] data = new int[32];
        for (int value = 0; value < codes.Length; value++)
        {
            codes[value] = BchCodec.CreateCode(value, 5, 10, 0x537, 0x4445);
            data[value] = value;
        }

        return BchCodec.CorrectRedundantCodes(
            firstCode,
            secondCode,
            15,
            codes,
            codes,
            data,
            3);
    }

    internal static int CreateLegacyModel1Code(int errorCorrectionIndex, int mask)
    {
        int data = GetErrorCorrectionBits(errorCorrectionIndex) << 3 | mask;
        LegacyModel1Table.GetFormatCode(errorCorrectionIndex, mask);
        return BchCodec.CreateCode(data, 5, 10, 0x537, 0x2825);
    }

    internal static BchCorrectionResult CorrectLegacyModel1Copies(
        int firstCode,
        int secondCode)
    {
        int[] codes = new int[32];
        int[] data = new int[32];
        int index = 0;
        for (int level = 0; level < 4; level++)
        {
            for (int mask = 0; mask < 8; mask++)
            {
                codes[index] = CreateLegacyModel1Code(level, mask);
                data[index] = level * 8 + mask;
                index++;
            }
        }

        return BchCodec.CorrectRedundantCodes(
            firstCode,
            secondCode,
            15,
            codes,
            codes,
            data,
            3);
    }

    internal static int CreateRectangularMicroFinderCode(
        int versionIndex,
        int errorCorrectionIndex)
    {
        int data = RectangularMicroFormatCodeTable.GetFormatData(
            versionIndex,
            errorCorrectionIndex);
        return BchCodec.CreateCode(data, 6, 12, 0x1F25, 0x1FAB2);
    }

    internal static int CreateRectangularMicroSubfinderCode(
        int versionIndex,
        int errorCorrectionIndex)
    {
        int data = RectangularMicroFormatCodeTable.GetFormatData(
            versionIndex,
            errorCorrectionIndex);
        return BchCodec.CreateCode(data, 6, 12, 0x1F25, 0x20A7B);
    }

    internal static BchCorrectionResult CorrectRectangularMicroCopies(
        int finderCode,
        int subfinderCode)
    {
        int[] finderCodes = new int[64];
        int[] subfinderCodes = new int[64];
        int[] data = new int[64];
        for (int value = 0; value < data.Length; value++)
        {
            finderCodes[value] = BchCodec.CreateCode(value, 6, 12, 0x1F25, 0x1FAB2);
            subfinderCodes[value] = BchCodec.CreateCode(value, 6, 12, 0x1F25, 0x20A7B);
            data[value] = value;
        }

        return BchCodec.CorrectRedundantCodes(
            finderCode,
            subfinderCode,
            18,
            finderCodes,
            subfinderCodes,
            data,
            3);
    }

    static int GetErrorCorrectionBits(int errorCorrectionIndex)
    {
        if (errorCorrectionIndex == EccBlockTable.Low)
        {
            return 1;
        }

        if (errorCorrectionIndex == EccBlockTable.Medium)
        {
            return 0;
        }

        if (errorCorrectionIndex == EccBlockTable.Quartile)
        {
            return 3;
        }

        if (errorCorrectionIndex == EccBlockTable.High)
        {
            return 2;
        }

        throw new QrInputException(
            "The error-correction index must be between zero and three.",
            "errorCorrectionIndex");
    }
}
