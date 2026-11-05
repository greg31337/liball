namespace xml;

/// <summary>
/// Receives XSLT messages and assertion events.
/// </summary>
public interface IXsltMessageReceiver
{
    /// <summary>
    /// Receives one XSLT message.
    /// </summary>
    /// <param name="message">The message data.</param>
    void ReceiveXsltMessage(XsltMessage message);
}
