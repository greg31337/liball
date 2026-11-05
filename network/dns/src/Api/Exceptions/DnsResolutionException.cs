namespace dns;

/// <summary>Represents a DNS stub or iterative resolution failure.</summary>
public class DnsResolutionException : System.Exception
{
    /// <summary>Initializes a DNS resolution exception with a descriptive message.</summary>
    /// <param name="message">The message describing the resolution failure.</param>
    public DnsResolutionException(string message)
        : base(message)
    {
    }
}
