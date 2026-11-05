namespace sip;

/// <summary>Holds REFER client body and authentication limits.</summary>
public class SipReferOptions
{
    /// <summary>The maximum REFER body size in bytes.</summary>
    public int MaxBodyBytes = SipDefaults.MaxBodyBytes;
    /// <summary>The maximum number of authentication retries.</summary>
    public int MaxAuthenticationAttempts = SipDefaults.MaxAuthenticationAttempts;
}
