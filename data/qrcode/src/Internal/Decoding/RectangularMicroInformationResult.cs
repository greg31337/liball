namespace qrcode;

internal class RectangularMicroInformationResult
{
    internal bool Success;

    internal QrFailureReason FailureReason;

    internal int DimensionVersionIndex = -1;

    // Zero-based table index; -1 until decoded size and matrix dimensions agree.
    internal int VersionIndex = -1;

    internal int Width;

    internal int Height;

    internal QrErrorCorrectionLevel ErrorCorrectionLevel = QrErrorCorrectionLevel.None;

    internal QrMaskPattern MaskPattern = QrMaskPattern.Automatic;

    internal MatrixInformationCopy[] FormatCopies = new MatrixInformationCopy[0];

    internal int SelectedFormatCopy = -1;

    internal bool FormatCopiesAgree;
}
