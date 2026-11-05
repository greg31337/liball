namespace fix;

/// <summary>Represents an unrecoverable dictionary operation failure.</summary>
public class FixDictionaryException : System.Exception
{
    /// <summary>Creates a dictionary exception with a caller-provided description.</summary>
    /// <param name="message">The failure description.</param>
    public FixDictionaryException(string message) : base(message)
    {
    }
}
