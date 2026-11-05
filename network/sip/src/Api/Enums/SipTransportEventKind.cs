namespace sip;

/// <summary>Identifies a transport event.</summary>
public enum SipTransportEventKind
{
    /// <summary>No event is available.</summary>
    None = 0,
    /// <summary>A local endpoint was bound.</summary>
    Bound = 1,
    /// <summary>A reliable connection was established.</summary>
    Connected = 2,
    /// <summary>A reliable connection was disconnected.</summary>
    Disconnected = 3,
    /// <summary>A SIP message was received.</summary>
    MessageReceived = 4,
    /// <summary>A SIP message was sent.</summary>
    MessageSent = 5,
    /// <summary>A send operation failed.</summary>
    SendFailed = 6,
    /// <summary>A keepalive was received.</summary>
    KeepAliveReceived = 7,
    /// <summary>An outbound flow failed.</summary>
    FlowFailed = 8,
    /// <summary>The transport was closed.</summary>
    Closed = 9,
    /// <summary>An application-provided transport content coding failed.</summary>
    ContentCodingFailed = 10
}
