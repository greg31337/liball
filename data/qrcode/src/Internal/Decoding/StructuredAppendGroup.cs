namespace qrcode;

internal class StructuredAppendGroup
{
    internal int Parity;
    internal int Total;
    internal string? ApplicationIdentifier;
    internal bool UsesFnc1First;
    internal int Fnc1SecondApplicationIndicator = -1;
    internal QrDecodeResult[] Parts = new QrDecodeResult[0];
    internal QrFailureReason FailureReason;
    internal global::System.Collections.Generic.List<QrDecodeResult> PendingParts = new global::System.Collections.Generic.List<QrDecodeResult>();
}
