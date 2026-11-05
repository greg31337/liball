namespace qrcode;

internal class StructuredAppendGroupingResult
{
    internal StructuredAppendGroup[] Groups = new StructuredAppendGroup[0];
    internal QrDecodeResult[] OrdinaryResults = new QrDecodeResult[0];
    internal QrDecodeResult[] RejectedResults = new QrDecodeResult[0];
    internal QrFailureReason FailureReason;
}
