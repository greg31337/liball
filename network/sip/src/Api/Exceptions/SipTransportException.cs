namespace sip;

/// <summary>Indicates that a SIP transport operation failed.</summary>
public class SipTransportException : System.Exception
{
    /// <summary>The transport on which the operation failed.</summary>
    public SipTransportKind Transport;
    /// <summary>The related connection identifier, when available.</summary>
    public string? ConnectionId;

    /// <summary>Initializes a SIP transport failure.</summary>
    /// <param name="message">The description of the failure.</param>
    /// <param name="transport">The transport on which the operation failed.</param>
    /// <param name="connectionId">The related connection identifier, when available.</param>
    public SipTransportException(string message, SipTransportKind transport, string? connectionId) : base(message)
    {
        Transport = transport;
        ConnectionId = connectionId;
    }
}
