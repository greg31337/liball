namespace odf;

/// <summary>
/// Represents a failure that prevents OpenDocument validation from completing.
/// </summary>
public class OdfValidationException : System.Exception
{
    /// <summary>
    /// Initializes an exception with a description of the validation failure.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    public OdfValidationException(string message) : base(message)
    {
    }
}
