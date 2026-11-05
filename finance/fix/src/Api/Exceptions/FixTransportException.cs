namespace fix;

/// <summary>Represents an unrecoverable session transport failure.</summary>
public class FixTransportException : System.Exception
{
    /// <summary>Creates a transport exception with a caller-provided description.</summary>
    /// <param name="message">The failure description.</param>
    public FixTransportException(string message) : base(message)
    {
    }
}
