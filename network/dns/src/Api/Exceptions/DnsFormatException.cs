namespace dns;

/// <summary>Represents invalid DNS wire or presentation-format data.</summary>
public class DnsFormatException : System.Exception
{
    /// <summary>Initializes a DNS format exception with a descriptive message.</summary>
    /// <param name="message">The message describing the invalid DNS data.</param>
    public DnsFormatException(string message)
        : base(message)
    {
    }
}
