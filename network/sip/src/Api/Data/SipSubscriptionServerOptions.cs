namespace sip;

/// <summary>Holds subscription-server acceptance, expiry, fork, and queue policy.</summary>
public class SipSubscriptionServerOptions
{
    /// <summary>The default expiry when SUBSCRIBE omits Expires.</summary>
    public int DefaultExpiresSeconds = SipDefaults.SubscriptionExpiresSeconds;
    /// <summary>The minimum nonzero subscription expiry.</summary>
    public int MinExpiresSeconds = 60;
    /// <summary>The maximum subscription expiry.</summary>
    public int MaxExpiresSeconds = 86400;
    /// <summary>The maximum SUBSCRIBE or NOTIFY body size in bytes.</summary>
    public int MaxBodyBytes = SipDefaults.MaxBodyBytes;
    /// <summary>The maximum number of live server subscriptions.</summary>
    public int MaxSubscriptions = SipDefaults.MaxQueueCount;
    /// <summary>The maximum number of queued server events.</summary>
    public int MaxEventCount = SipDefaults.MaxQueueCount;
    /// <summary>Allows requests identified by routing as forked subscriptions.</summary>
    public bool AllowForking = true;
    /// <summary>Requires a configured application authorization policy.</summary>
    public bool RequireAuthorization;
    /// <summary>The initial state reported after authorization.</summary>
    public SipSubscriptionState InitialState = SipSubscriptionState.Active;
}
