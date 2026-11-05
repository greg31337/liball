namespace fix;

/// <summary>Represents an unrecoverable durable message-store failure.</summary>
public class FixStoreException : System.Exception
{
    /// <summary>Creates a store exception with a caller-provided description.</summary>
    /// <param name="message">The failure description.</param>
    public FixStoreException(string message) : base(message)
    {
    }
}
