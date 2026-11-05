namespace fix;

/// <summary>Represents an unrecoverable Orchestra processing failure.</summary>
public class OrchestraException : System.Exception
{
    /// <summary>Creates an Orchestra exception with a caller-provided description.</summary>
    /// <param name="message">The failure description.</param>
    public OrchestraException(string message) : base(message)
    {
    }
}
