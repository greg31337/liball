namespace qrcode;

internal class MatrixCodewordResult
{
    internal bool Success;

    internal QrFailureReason FailureReason;

    internal QrSymbolKind SymbolKind;

    // Numbered version for square profiles, zero-based size index for rMQR.
    internal int Version = -1;

    internal QrErrorCorrectionLevel ErrorCorrectionLevel = QrErrorCorrectionLevel.None;

    internal QrMaskPattern MaskPattern = QrMaskPattern.Automatic;

    internal int DataBitCount;

    internal int RemainderBitCount;

    internal int InvalidRemainderBitCount;

    internal int UnknownPayloadModuleCount;

    internal int LowConfidencePayloadModuleCount;

    // Unique placement-order positions, before block deinterleaving.
    internal int[] ErasurePositions = new int[0];

    internal byte[] ExtractedCodewords = new byte[0];

    // Available only after successful repair and profile-specific checks.
    internal byte[] DataCodewords = new byte[0];

    internal BlockCorrectionResult? Correction;
}
