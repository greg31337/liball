namespace websocket;

/// <summary>
/// Identifies an event emitted by a WebSocket connection.
/// </summary>
public enum WebSocketEventType
{
    /// <summary>
    /// Begins a streamed application message.
    /// </summary>
    MessageStart = 0,

    /// <summary>
    /// Carries application message data.
    /// </summary>
    MessageData = 1,

    /// <summary>
    /// Completes a streamed application message.
    /// </summary>
    MessageEnd = 2,

    /// <summary>
    /// Reports a received Ping control message.
    /// </summary>
    Ping = 3,

    /// <summary>
    /// Reports a received Pong control message.
    /// </summary>
    Pong = 4,

    /// <summary>
    /// Reports acknowledgement of a tracked Ping.
    /// </summary>
    PingAcknowledged = 5,

    /// <summary>
    /// Reports a received or completed closing handshake.
    /// </summary>
    Close = 6,

    /// <summary>
    /// Reports a protocol, state, limit, or transport error.
    /// </summary>
    Error = 7,

    /// <summary>
    /// Reports clean end-of-stream, EOF, or a transport reset.
    /// </summary>
    TransportEnd = 8
}
