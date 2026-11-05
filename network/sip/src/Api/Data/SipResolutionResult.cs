namespace sip;

/// <summary>Holds the outcome of SIP server resolution.</summary>
public class SipResolutionResult
{
    /// <summary>The resolution outcome.</summary>
    public SipResolutionStatus Status;
    /// <summary>The ordered server candidates.</summary>
    public SipServerCandidate[] Candidates = new SipServerCandidate[0];
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
