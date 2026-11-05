namespace asn1;

internal class BerLengthAccountingResult
{
    internal bool Success;
    internal bool LimitExceeded;
    internal int CompletedScopeCount;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
