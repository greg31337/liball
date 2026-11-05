namespace fix;

/// <summary>Represents misuse or an unrecoverable message validation failure.</summary>
public class FixValidationException : System.Exception
{
    /// <summary>Creates a validation exception with a caller-provided description.</summary>
    /// <param name="message">The failure description.</param>
    public FixValidationException(string message) : base(message)
    {
    }
}
