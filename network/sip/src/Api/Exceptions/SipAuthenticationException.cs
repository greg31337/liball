namespace sip;

/// <summary>Indicates that SIP authentication failed.</summary>
public class SipAuthenticationException : System.Exception
{
    /// <summary>The authentication status associated with the failure.</summary>
    public SipAuthenticationStatus Status;
    /// <summary>The authentication realm, when available.</summary>
    public string? Realm;

    /// <summary>Initializes a SIP authentication failure.</summary>
    /// <param name="message">The description of the failure.</param>
    /// <param name="status">The authentication status associated with the failure.</param>
    /// <param name="realm">The authentication realm, when available.</param>
    public SipAuthenticationException(string message, SipAuthenticationStatus status, string? realm) : base(message)
    {
        Status = status;
        Realm = realm;
    }
}
