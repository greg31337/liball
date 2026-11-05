namespace odf;

/// <summary>
/// Represents a failure while loading, creating, or saving an OpenDocument document.
/// </summary>
public class OdfDocumentException : System.Exception
{
    /// <summary>
    /// Initializes an exception with a description of the document failure.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    public OdfDocumentException(string message) : base(message)
    {
    }
}
