namespace qrcode;

internal class MicroInformationResult
{
    internal bool Success;

    internal QrFailureReason FailureReason;

    internal int DimensionVersion;

    // Zero until the format version agrees with the matrix dimensions.
    internal int Version;

    internal QrErrorCorrectionLevel ErrorCorrectionLevel = QrErrorCorrectionLevel.None;

    internal QrMaskPattern MaskPattern = QrMaskPattern.Automatic;

    // Micro QR has one 15-bit field, not two redundant copies.
    internal MatrixInformationCopy FormatCopy = new MatrixInformationCopy();
}
