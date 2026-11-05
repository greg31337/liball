namespace dns;

/// <summary>Represents a DNS transport or connection failure.</summary>
public class DnsTransportException : System.Exception
{
    /// <summary>Initializes a DNS transport exception with a descriptive message.</summary>
    /// <param name="message">The message describing the transport failure.</param>
    public DnsTransportException(string message)
        : base(message)
    {
    }
}
