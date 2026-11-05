namespace sip;

internal class SipTransactionAdmissionResult
{
    internal bool Accepted;
    internal bool Existing;
    internal bool Merged;
    internal bool Malformed;
    internal bool Overloaded;
    internal int StatusCode;
    internal string? TransactionId;
    internal string? ErrorMessage;
}
