namespace qrcode;

internal class Model2InformationReader
{
    // Reads one canonical, polarity-normalized Model 2 matrix. Orientation and
    // alternate-copy retries belong to the recovery pipeline. BCH correction is
    // ordinary decoding and is not disabled by the optional recovery flag.
    internal static Model2InformationResult Read(QrMatrix matrix, QrDecodeOptions options)
    {
        return ReadWithCopyChoices(matrix, options, -1, -1);
    }

    internal static Model2InformationResult ReadWithCopyChoices(QrMatrix matrix, QrDecodeOptions options, int formatCopy, int versionCopy)
    {
        MatrixInformationSupport.ValidateInput(matrix, options);
        Model2InformationResult result = new Model2InformationResult();
        int width = matrix.Width - matrix.QuietZoneLeft - matrix.QuietZoneRight;
        int height = matrix.Height - matrix.QuietZoneTop - matrix.QuietZoneBottom;
        if (width != height || width < 21 || width > 177 || (width - 17) % 4 != 0)
        {
            result.FailureReason = QrFailureReason.InvalidVersion;
            return result;
        }

        int version = (width - 17) / 4;
        result.DimensionVersion = version;
        result.FormatCopies = new MatrixInformationCopy[]
        {
            ReadCopy(matrix, version, false, false), ReadCopy(matrix, version, false, true)
        };
        result.SelectedFormatCopy = MatrixInformationSupport.ChooseRequestedCopy(result.FormatCopies, formatCopy);
        result.FormatCopiesAgree = MatrixInformationSupport.CopiesAgree(result.FormatCopies);
        if (result.SelectedFormatCopy >= 0)
        {
            int data = result.FormatCopies[result.SelectedFormatCopy].Data;
            result.ErrorCorrectionLevel = (QrErrorCorrectionLevel)(data / 8);
            result.MaskPattern = (QrMaskPattern)(data % 8);
        }

        if (version < 7)
        {
            result.Version = version;
        }
        else
        {
            result.VersionCopies = new MatrixInformationCopy[]
            {
                ReadCopy(matrix, version, true, false), ReadCopy(matrix, version, true, true)
            };
            result.SelectedVersionCopy = MatrixInformationSupport.ChooseRequestedCopy(result.VersionCopies, versionCopy);
            result.VersionCopiesAgree = MatrixInformationSupport.CopiesAgree(result.VersionCopies);
            if (result.SelectedVersionCopy >= 0 && result.VersionCopies[result.SelectedVersionCopy].Data == version)
            {
                result.Version = version;
            }
        }

        if (result.SelectedFormatCopy < 0)
        {
            result.FailureReason = QrFailureReason.InvalidFormat;
        }
        else if (result.Version == 0)
        {
            result.FailureReason = QrFailureReason.InvalidVersion;
        }
        else
        {
            result.Success = true;
        }

        return result;
    }

    static MatrixInformationCopy ReadCopy(QrMatrix matrix, int version, bool versionInformation, bool secondary)
    {
        MatrixInformationCopy copy = new MatrixInformationCopy();
        int bitCount = versionInformation ? 18 : 15;
        for (int bit = 0; bit < bitCount; bit++)
        {
            int row;
            int column;
            if (versionInformation)
            {
                row = secondary ? Model2PlacementTable.GetVersionSecondaryRow(version, bit)
                    : Model2PlacementTable.GetVersionPrimaryRow(version, bit);
                column = secondary ? Model2PlacementTable.GetVersionSecondaryColumn(version, bit)
                    : Model2PlacementTable.GetVersionPrimaryColumn(version, bit);
            }
            else
            {
                row = secondary ? Model2PlacementTable.GetFormatSecondaryRow(version, bit)
                    : Model2PlacementTable.GetFormatPrimaryRow(bit);
                column = secondary ? Model2PlacementTable.GetFormatSecondaryColumn(version, bit)
                    : Model2PlacementTable.GetFormatPrimaryColumn(bit);
            }

            MatrixInformationSupport.ReadBit(matrix, copy, row, column, bit);
        }

        MatrixInformationSupport.CorrectCopy(copy, bitCount, versionInformation ? 7 : 0, versionInformation ? 40 : 31,
            data => versionInformation ? VersionCodeTable.GetCode(data) : FormatCodeTable.GetCode(data / 8, data % 8));
        return copy;
    }
}
