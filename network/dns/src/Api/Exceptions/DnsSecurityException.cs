namespace dns;

/// <summary>Represents a DNSSEC, TSIG, or zone-digest security failure.</summary>
public class DnsSecurityException : System.Exception
{
    /// <summary>Initializes a DNS security exception with a descriptive message.</summary>
    /// <param name="message">The message describing the security failure.</param>
    public DnsSecurityException(string message)
        : base(message)
    {
    }
}
