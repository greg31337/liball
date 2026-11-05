namespace qrcode;

internal class MatrixInformationCopy
{
    internal int ReceivedCode;

    internal int KnownBitMask;

    internal int MissingBitCount;

    internal bool Success;

    // Model 2 format data is ECC index * 8 + mask; version data is the version.
    // Micro data is symbol number * 4 + mask. rMQR data is ECC index * 32 + size index.
    internal int Data = -1;

    internal int CorrectedCode = -1;

    internal int CorrectedErrorCount = -1;
}
