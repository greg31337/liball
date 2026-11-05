namespace qrcode;

internal class Model2InformationResult
{
    internal bool Success;

    internal QrFailureReason FailureReason;

    internal int DimensionVersion;

    // Zero until version information is accepted and agrees with dimensions.
    internal int Version;

    internal QrErrorCorrectionLevel ErrorCorrectionLevel = QrErrorCorrectionLevel.None;

    internal QrMaskPattern MaskPattern = QrMaskPattern.Automatic;

    internal MatrixInformationCopy[] FormatCopies = new MatrixInformationCopy[0];

    // Empty for versions 1-6, which have no explicit version information.
    internal MatrixInformationCopy[] VersionCopies = new MatrixInformationCopy[0];

    internal int SelectedFormatCopy = -1;

    internal int SelectedVersionCopy = -1;

    internal bool FormatCopiesAgree;

    internal bool VersionCopiesAgree;
}
