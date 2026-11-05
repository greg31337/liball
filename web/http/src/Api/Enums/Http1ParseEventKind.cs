namespace http;

/// <summary>
/// Identifies an event emitted while parsing HTTP/1 input.
/// </summary>
public enum Http1ParseEventKind
{
    /// <summary>
    /// A complete request or response head is available.
    /// </summary>
    MessageHead = 0,

    /// <summary>
    /// A segment of message content is available.
    /// </summary>
    Content = 1,

    /// <summary>
    /// A trailing field section is available.
    /// </summary>
    Trailers = 2,

    /// <summary>
    /// The current message is complete.
    /// </summary>
    Complete = 3,

    /// <summary>
    /// The connection switched to another protocol.
    /// </summary>
    ProtocolSwitch = 4,

    /// <summary>
    /// Opaque tunnel bytes are available.
    /// </summary>
    TunnelBytes = 5
}
