namespace sip;

internal class SipReliableOperationResult
{
    internal bool Success;
    internal bool Backpressured;
    internal int BytesAccepted;
    internal string? ErrorMessage;
}
