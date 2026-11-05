namespace sip;

/// <summary>Holds registration lifetime, retry, and refresh policy.</summary>
public class SipRegistrationOptions
{
    /// <summary>The requested expiry in seconds.</summary>
    public int ExpiresSeconds = SipDefaults.RegistrationExpiresSeconds;
    /// <summary>The minimum accepted expiry in seconds.</summary>
    public int MinExpiresSeconds = 60;
    /// <summary>The maximum accepted expiry in seconds.</summary>
    public int MaxExpiresSeconds = 86400;
    /// <summary>The refresh lead time in seconds.</summary>
    public int RefreshBeforeSeconds = 60;
    /// <summary>The maximum random refresh advance in seconds.</summary>
    public int RefreshJitterSeconds = 5;
    /// <summary>The default delay before retrying a transient failure.</summary>
    public int RetryDelaySeconds = 5;
    /// <summary>The maximum authentication attempts.</summary>
    public int MaxAuthenticationAttempts = SipDefaults.MaxAuthenticationAttempts;
    /// <summary>Allows registration redirects to be followed.</summary>
    public bool FollowRedirects = true;
    /// <summary>The maximum number of registration redirects.</summary>
    public int MaxRedirects = 3;
    /// <summary>Enables RFC 5626 Outbound behavior.</summary>
    public bool UseOutbound;
    /// <summary>Requests RFC 5627 GRUU assignment for instance-identified Contacts.</summary>
    public bool UseGruu;
}
