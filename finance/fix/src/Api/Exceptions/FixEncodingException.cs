namespace fix;

/// <summary>Represents an unrecoverable FIX encoding or decoding failure.</summary>
public class FixEncodingException : System.Exception
{
    /// <summary>Creates an encoding exception with a caller-provided description.</summary>
    /// <param name="message">The failure description.</param>
    public FixEncodingException(string message) : base(message)
    {
    }
}
