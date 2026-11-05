namespace qrcode;

internal class MicroInformationReader
{
    static readonly int[] Versions = new int[] { 1, 2, 2, 3, 3, 4, 4, 4 };

    static readonly int[] ErrorCorrectionIndices = new int[] { -1, 0, 1, 0, 1, 0, 1, 2 };

    // One canonical, polarity-normalized matrix, excluding declared quiet zones.
    // Decode all symbol numbers before checking size; dimensions must not turn
    // an independently valid but inconsistent format word into an accepted one.
    internal static MicroInformationResult Read(QrMatrix matrix, QrDecodeOptions options)
    {
        MatrixInformationSupport.ValidateInput(matrix, options);
        MicroInformationResult result = new MicroInformationResult();
        int width = matrix.Width - matrix.QuietZoneLeft - matrix.QuietZoneRight;
        int height = matrix.Height - matrix.QuietZoneTop - matrix.QuietZoneBottom;
        if (width != height || width < 11 || width > 17 || width % 2 != 1)
        {
            result.FailureReason = QrFailureReason.InvalidVersion;
            return result;
        }

        result.DimensionVersion = (width - 9) / 2;
        MatrixInformationCopy copy = result.FormatCopy;
        for (int bit = 0; bit < 15; bit++)
        {
            int row = bit <= 7 ? MicroPlacementTable.GetFormatVerticalRow(bit)
                : MicroPlacementTable.GetFormatHorizontalRow(bit);
            int column = bit <= 7 ? MicroPlacementTable.GetFormatVerticalColumn(bit)
                : MicroPlacementTable.GetFormatHorizontalColumn(bit);
            MatrixInformationSupport.ReadBit(matrix, copy, row, column, bit);
        }

        MatrixInformationSupport.CorrectCopy(copy, 15, 0, 31,
            data => MicroFormatCodeTable.GetCode(Versions[data / 4], ErrorCorrectionIndices[data / 4], data % 4));
        if (!copy.Success)
        {
            result.FailureReason = QrFailureReason.InvalidFormat;
            return result;
        }

        int symbolNumber = copy.Data / 4;
        int version = Versions[symbolNumber];
        if (version != result.DimensionVersion)
        {
            result.FailureReason = QrFailureReason.InvalidVersion;
            return result;
        }

        result.Version = version;
        result.ErrorCorrectionLevel = (QrErrorCorrectionLevel)ErrorCorrectionIndices[symbolNumber];
        result.MaskPattern = (QrMaskPattern)(copy.Data % 4);
        result.Success = true;
        return result;
    }
}
