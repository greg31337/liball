namespace sip;

/// <summary>Holds one pager-mode MESSAGE event.</summary>
public class SipInstantMessageEvent
{
    /// <summary>The event kind.</summary>
    public SipInstantMessageEventKind Kind;
    /// <summary>The instant-message request.</summary>
    public SipInstantMessageRequest? Request;
    /// <summary>The sent or received SIP message.</summary>
    public SipMessage? Message;
    /// <summary>The final response.</summary>
    public SipMessage? Response;
    /// <summary>Indicates successful delivery.</summary>
    public bool Success;
    /// <summary>Indicates that no further response is expected for this event.</summary>
    public bool Final;
    /// <summary>Indicates that the MESSAGE belongs to an existing dialog.</summary>
    public bool InDialog;
    /// <summary>The associated SIP response status code, or zero when absent.</summary>
    public int StatusCode;
    /// <summary>The number of authentication retries performed.</summary>
    public int AuthenticationAttemptCount;
    /// <summary>The number of redirects followed.</summary>
    public int RedirectCount;
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
