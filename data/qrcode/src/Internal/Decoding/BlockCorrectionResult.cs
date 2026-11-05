namespace qrcode;

internal class BlockCorrectionResult
{
    internal bool Success;

    internal QrFailureReason FailureReason;

    internal byte[] DataCodewords = new byte[0];

    internal byte[] Codewords = new byte[0];

    internal QrCorrectionResult[] BlockResults = new QrCorrectionResult[0];

    internal int CorrectedErrorCount;

    internal int CorrectedErasureCount;
}
