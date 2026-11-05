namespace sip;

internal class SipDnsCandidateResult
{
    internal SipResolutionStatus Status;
    internal SipServerCandidate[] Candidates = new SipServerCandidate[0];
    internal bool FoundServiceRecords;
    internal string? ErrorMessage;
}
