namespace sip;

/// <summary>Holds publication-server expiry, size, authorization, and queue policy.</summary>
public class SipPublicationServerOptions
{
    /// <summary>The expiry used when a PUBLISH request omits Expires.</summary>
    public int DefaultExpiresSeconds = SipDefaults.SubscriptionExpiresSeconds;
    /// <summary>The minimum nonzero publication expiry.</summary>
    public int MinExpiresSeconds = 60;
    /// <summary>The maximum publication expiry.</summary>
    public int MaxExpiresSeconds = 86400;
    /// <summary>The maximum publication body size in bytes.</summary>
    public int MaxBodyBytes = SipDefaults.MaxBodyBytes;
    /// <summary>The maximum number of stored publications.</summary>
    public int MaxPublications = SipDefaults.MaxQueueCount;
    /// <summary>The maximum number of queued publication events.</summary>
    public int MaxEventCount = SipDefaults.MaxQueueCount;
    /// <summary>Requires a configured application authorization policy.</summary>
    public bool RequireAuthorization;
    /// <summary>Validates bodies through the configured event-package registry.</summary>
    public bool ValidateEventBodies = true;
}
