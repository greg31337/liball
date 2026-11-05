namespace fix;

/// <summary>Represents an unrecoverable FIX session operation failure.</summary>
public class FixSessionException : System.Exception
{
    /// <summary>Creates a session exception with a caller-provided description.</summary>
    /// <param name="message">The failure description.</param>
    public FixSessionException(string message) : base(message)
    {
    }
}
