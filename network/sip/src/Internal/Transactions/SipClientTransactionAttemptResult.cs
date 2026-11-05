namespace sip;

internal class SipClientTransactionAttemptResult
{
    internal bool Created;
    internal bool Retried;
    internal bool Suppressed;
    internal bool Exhausted;
    internal int CandidateIndex = -1;
    internal SipServerCandidate? Candidate;
    internal SipSendRequest? SendRequest;
    internal string? Branch;
    internal string? ErrorMessage;
}
