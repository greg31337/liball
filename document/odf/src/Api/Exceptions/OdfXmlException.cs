namespace odf;

/// <summary>
/// Represents a failure while reading, validating, or writing OpenDocument XML.
/// </summary>
public class OdfXmlException : System.Exception
{
    /// <summary>
    /// Initializes an exception with a description of the XML failure.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    public OdfXmlException(string message) : base(message)
    {
    }
}
