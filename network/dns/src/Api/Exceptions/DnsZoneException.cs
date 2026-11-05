namespace dns;

/// <summary>Represents a DNS zone parsing, validation, or mutation failure.</summary>
public class DnsZoneException : System.Exception
{
    /// <summary>Initializes a DNS zone exception with a descriptive message.</summary>
    /// <param name="message">The message describing the zone failure.</param>
    public DnsZoneException(string message)
        : base(message)
    {
    }
}
