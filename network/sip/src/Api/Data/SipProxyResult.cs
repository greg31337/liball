namespace sip;

/// <summary>Holds the current or final result of proxy processing.</summary>
public class SipProxyResult
{
    /// <summary>Indicates successful processing.</summary>
    public bool Success;
    /// <summary>The selected final response.</summary>
    public SipMessage? Response;
    /// <summary>The transformed outbound request.</summary>
    public SipMessage? Request;
    /// <summary>The transformed outbound branch requests.</summary>
    public SipMessage[] Requests = new SipMessage[0];
    /// <summary>The transport send operation for the transformed message.</summary>
    public SipSendRequest? SendRequest;
    /// <summary>The transport send operations for the branch requests.</summary>
    public SipSendRequest[] SendRequests = new SipSendRequest[0];
    /// <summary>The created branch targets.</summary>
    public SipProxyTarget[] Targets = new SipProxyTarget[0];
    /// <summary>The response status code, or zero when absent.</summary>
    public int StatusCode;
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>The number of active stateful branches.</summary>
    public int ActiveBranchCount;
    /// <summary>The active Timer C deadlines.</summary>
    public long[] TimerCDeadlines = new long[0];
    /// <summary>Indicates that stateful processing completed.</summary>
    public bool Completed;
    /// <summary>Indicates that the selected response represents a timeout.</summary>
    public bool TimedOut;
    /// <summary>The authenticated and authorized proxy identity.</summary>
    public string? AuthenticatedIdentity;
}
