namespace odf;

/// <summary>
/// Represents a failure while applying an edit to OpenDocument content.
/// </summary>
public class OdfEditException : System.Exception
{
    /// <summary>
    /// Initializes an exception with a description of the edit failure.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    public OdfEditException(string message) : base(message)
    {
    }
}
