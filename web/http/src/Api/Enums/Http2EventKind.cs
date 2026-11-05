namespace http;

/// <summary>
/// Identifies an event delivered by HTTP/2 processing.
/// </summary>
public enum Http2EventKind
{
    /// <summary>
    /// Connection initialization completed.
    /// </summary>
    ConnectionReady = 0,

    /// <summary>
    /// A decoded frame was received.
    /// </summary>
    FrameReceived = 1,

    /// <summary>
    /// A request or response head was delivered.
    /// </summary>
    MessageHead = 2,

    /// <summary>
    /// Message content was delivered.
    /// </summary>
    Content = 3,

    /// <summary>
    /// A trailing field section was delivered.
    /// </summary>
    Trailers = 4,

    /// <summary>
    /// A stream completed normally.
    /// </summary>
    StreamComplete = 5,

    /// <summary>
    /// A stream was reset.
    /// </summary>
    StreamReset = 6,

    /// <summary>
    /// Peer settings changed.
    /// </summary>
    SettingsChanged = 7,

    /// <summary>
    /// A ping acknowledgement was received.
    /// </summary>
    PingAcknowledged = 8,

    /// <summary>
    /// A pushed request was promised.
    /// </summary>
    PushPromised = 9,

    /// <summary>
    /// A stream or connection error occurred.
    /// </summary>
    Error = 10
}
