namespace qrcode;

internal class RectangularMicroInformationReader
{
    // Coordinates are canonical (width greater than height), with normalized
    // polarity. Rotation/reflection and alternate-copy recovery are separate stages.
    internal static RectangularMicroInformationResult Read(QrMatrix matrix, QrDecodeOptions options)
    {
        return ReadWithCopyChoices(matrix, options, -1);
    }

    internal static RectangularMicroInformationResult ReadWithCopyChoices(QrMatrix matrix, QrDecodeOptions options, int formatCopy)
    {
        MatrixInformationSupport.ValidateInput(matrix, options);
        RectangularMicroInformationResult result = new RectangularMicroInformationResult();
        int width = matrix.Width - matrix.QuietZoneLeft - matrix.QuietZoneRight;
        int height = matrix.Height - matrix.QuietZoneTop - matrix.QuietZoneBottom;
        for (int index = RectangularMicroVersionTable.MinimumVersionIndex;
            index <= RectangularMicroVersionTable.MaximumVersionIndex; index++)
        {
            if (RectangularMicroVersionTable.GetWidth(index) == width
                && RectangularMicroVersionTable.GetHeight(index) == height)
            {
                result.DimensionVersionIndex = index;
                break;
            }
        }

        if (result.DimensionVersionIndex < 0)
        {
            result.FailureReason = QrFailureReason.InvalidVersion;
            return result;
        }

        result.FormatCopies = new MatrixInformationCopy[]
        {
            ReadCopy(matrix, result.DimensionVersionIndex, false), ReadCopy(matrix, result.DimensionVersionIndex, true)
        };
        result.SelectedFormatCopy = MatrixInformationSupport.ChooseRequestedCopy(result.FormatCopies, formatCopy);
        result.FormatCopiesAgree = MatrixInformationSupport.CopiesAgree(result.FormatCopies);
        if (result.SelectedFormatCopy < 0)
        {
            result.FailureReason = QrFailureReason.InvalidFormat;
            return result;
        }

        int data = result.FormatCopies[result.SelectedFormatCopy].Data;
        int versionIndex = data % 32;
        if (versionIndex != result.DimensionVersionIndex)
        {
            result.FailureReason = QrFailureReason.InvalidVersion;
            return result;
        }

        result.VersionIndex = versionIndex;
        result.Width = width;
        result.Height = height;
        result.ErrorCorrectionLevel = data < 32 ? QrErrorCorrectionLevel.Medium : QrErrorCorrectionLevel.High;
        result.MaskPattern = QrMaskPattern.Four;
        result.Success = true;
        return result;
    }

    static MatrixInformationCopy ReadCopy(QrMatrix matrix, int versionIndex, bool subfinder)
    {
        MatrixInformationCopy copy = new MatrixInformationCopy();
        for (int bit = 0; bit < 18; bit++)
        {
            int row = subfinder ? RectangularMicroPlacementTable.GetSubfinderSideFormatRow(versionIndex, bit)
                : RectangularMicroPlacementTable.GetFinderSideFormatRow(bit);
            int column = subfinder ? RectangularMicroPlacementTable.GetSubfinderSideFormatColumn(versionIndex, bit)
                : RectangularMicroPlacementTable.GetFinderSideFormatColumn(bit);
            MatrixInformationSupport.ReadBit(matrix, copy, row, column, bit);
        }

        MatrixInformationSupport.CorrectCopy(copy, 18, 0, 63,
            data => subfinder ? RectangularMicroFormatCodeTable.GetSubfinderSideCode(data % 32, data / 32)
                : RectangularMicroFormatCodeTable.GetFinderSideCode(data % 32, data / 32));
        return copy;
    }
}
