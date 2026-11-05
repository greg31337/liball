namespace qrcode;

internal class StructuredAppendAssemblyResult
{
    internal bool Success;
    internal QrFailureReason FailureReason;
    internal QrDecodeResult? Result;
    // Borrowed observations, one representative per present index, in index order.
    internal QrDecodeResult[] OrderedParts = new QrDecodeResult[0];
    internal int[] MissingIndices = new int[0];
    internal int[] ConflictingIndices = new int[0];
    internal int ExpectedPartCount;
    internal int ActualPartCount;
    internal int DuplicateCount;
    internal int Parity = -1;
}
