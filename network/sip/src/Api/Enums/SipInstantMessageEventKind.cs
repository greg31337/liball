namespace sip;

/// <summary>Identifies a pager-mode MESSAGE lifecycle event.</summary>
public enum SipInstantMessageEventKind
{
    /// <summary>No event is available.</summary>
    None = 0,
    /// <summary>An outbound MESSAGE request was created.</summary>
    RequestCreated = 1,
    /// <summary>A provisional response was received.</summary>
    ProvisionalResponse = 2,
    /// <summary>An authenticated or redirected retry was created.</summary>
    RetryCreated = 3,
    /// <summary>The peer accepted the outbound MESSAGE.</summary>
    Delivered = 4,
    /// <summary>An incoming MESSAGE was accepted.</summary>
    Received = 5,
    /// <summary>The MESSAGE operation failed or was rejected.</summary>
    Failed = 6
}
