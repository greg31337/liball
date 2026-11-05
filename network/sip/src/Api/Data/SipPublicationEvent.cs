namespace sip;

/// <summary>Holds one queued publication event.</summary>
public class SipPublicationEvent
{
    /// <summary>The stable publication identifier.</summary>
    public string? PublicationId;
    /// <summary>The publication state.</summary>
    public SipPublicationState State;
    /// <summary>The publication request.</summary>
    public SipPublicationRequest? Request;
    /// <summary>The stored publication snapshot when one remains.</summary>
    public SipPublication? Publication;
    /// <summary>The associated SIP message.</summary>
    public SipMessage? Message;
    /// <summary>The received response.</summary>
    public SipMessage? Response;
    /// <summary>The response status code, or zero when absent.</summary>
    public int StatusCode;
    /// <summary>Indicates that the operation was accepted.</summary>
    public bool Accepted;
    /// <summary>Indicates that this publication operation is final.</summary>
    public bool Final;
    /// <summary>Indicates that aggregate event state should be recomputed and notified.</summary>
    public bool AggregationRequired;
    /// <summary>The current entity tag.</summary>
    public string? EntityTag;
    /// <summary>The remaining expiry in seconds, or negative one when unavailable.</summary>
    public int ExpiresSeconds = -1;
    /// <summary>The next monotonic refresh timestamp, or negative one when absent.</summary>
    public long RefreshAtMilliseconds = -1;
    /// <summary>The monotonic expiry timestamp, or negative one when absent.</summary>
    public long ExpiresAtMilliseconds = -1;
    /// <summary>The number of authentication retries performed.</summary>
    public int AuthenticationAttemptCount;
    /// <summary>The event reason.</summary>
    public string? Reason;
    /// <summary>The application authorization decision, when evaluated.</summary>
    public SipAuthorizationDecision? Authorization;
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
