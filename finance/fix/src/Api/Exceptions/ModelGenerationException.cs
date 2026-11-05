namespace fix;

/// <summary>Represents an unrecoverable model or schema generation failure.</summary>
public class ModelGenerationException : System.Exception
{
    /// <summary>Creates a generation exception with a caller-provided description.</summary>
    /// <param name="message">The failure description.</param>
    public ModelGenerationException(string message) : base(message)
    {
    }
}
