namespace asn1;

internal class EcnPlanExecutionResult
{
    internal AsnDecodeStatus Status;
    internal AsnRuntimeValue? Value;
    internal byte[] Data = [];
    internal int BitCount;
    internal int ConsumedBitCount;
    internal string PendingStepId = "";
    internal string PendingPath = "";
    internal bool HasBitOffset;
    internal int BitOffset;
    internal AsnSourceSpan? SourceSpan;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
