namespace sip;

/// <summary>Holds INVITE usage, extension, and session-timer policy.</summary>
public class SipInviteOptions
{
    /// <summary>Enables reliable provisional responses.</summary>
    public bool ReliableProvisional;
    /// <summary>Enables session timer negotiation and automatic refresh.</summary>
    public bool SessionTimers = true;
    /// <summary>The requested session interval in seconds, at least the minimum.</summary>
    public int SessionExpiresSeconds = 1800;
    /// <summary>The minimum session interval in seconds, never less than 90.</summary>
    public int MinSessionExpiresSeconds = 90;
    /// <summary>The maximum followed redirects.</summary>
    public int MaxRedirectCount = SipDefaults.MaxRedirectCount;
    /// <summary>Allows automatic authentication retries.</summary>
    public bool AllowAuthenticationRetry = true;
}
