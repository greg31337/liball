namespace sip;

/// <summary>Holds validated subscription information for application authorization.</summary>
public class SipSubscriptionAuthorizationContext
{
    /// <summary>The incoming SUBSCRIBE request.</summary>
    public SipMessage? Message;
    /// <summary>The requested event package.</summary>
    public string? EventPackage;
    /// <summary>The requested event identifier.</summary>
    public string? EventId;
    /// <summary>The matched dialog for a refresh or unsubscribe.</summary>
    public SipDialogId? DialogId;
    /// <summary>The requested expiry in seconds.</summary>
    public int ExpiresSeconds = -1;
    /// <summary>Indicates a refresh of an existing subscription.</summary>
    public bool Refresh;
    /// <summary>Indicates an unsubscribe request.</summary>
    public bool Unsubscribe;
    /// <summary>Indicates that routing identified a forked subscription.</summary>
    public bool Forked;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
