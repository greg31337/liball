namespace odf;

/// <summary>
/// Represents a failure while protecting or verifying OpenDocument content.
/// </summary>
public class OdfSecurityException : System.Exception
{
    /// <summary>
    /// Initializes an exception with a description of the security failure.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    public OdfSecurityException(string message) : base(message)
    {
    }
}
