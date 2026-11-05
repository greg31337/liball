namespace websocket;

/// <summary>
/// Holds the public classification of one connection event.
/// </summary>
public class WebSocketEvent
{
    /// <summary>
    /// Identifies the event category.
    /// </summary>
    public WebSocketEventType Type;

    /// <summary>
    /// Indicates whether <see cref="MessageType"/> applies to this event.
    /// </summary>
    public bool HasMessageType;

    /// <summary>
    /// Identifies the application message type for message events.
    /// </summary>
    public WebSocketMessageType MessageType;

    /// <summary>
    /// Holds message or control payload bytes relinquished by the connection to this event.
    /// </summary>
    public byte[] Payload = [];

    /// <summary>
    /// Holds the zero-based start of valid event bytes in <see cref="Payload"/>.
    /// </summary>
    public int PayloadOffset;

    /// <summary>
    /// Holds the number of valid event bytes beginning at <see cref="PayloadOffset"/>.
    /// </summary>
    public int PayloadCount;

    /// <summary>
    /// Indicates the first delivered chunk of a message.
    /// </summary>
    public bool First;

    /// <summary>
    /// Indicates the final delivered chunk of a message.
    /// </summary>
    public bool Final;

    /// <summary>
    /// Indicates whether <see cref="PingElapsedMilliseconds"/> contains Ping acknowledgement timing.
    /// </summary>
    public bool HasPingElapsedMilliseconds;

    /// <summary>
    /// Holds elapsed Ping round-trip milliseconds when <see cref="HasPingElapsedMilliseconds"/> is true.
    /// </summary>
    public long PingElapsedMilliseconds;

    /// <summary>
    /// Indicates whether <see cref="Close"/> contains close-handshake details.
    /// </summary>
    public bool HasClose;

    /// <summary>
    /// Holds close-handshake details when <see cref="HasClose"/> is true.
    /// </summary>
    public WebSocketCloseInfo Close = new WebSocketCloseInfo();

    /// <summary>
    /// Indicates whether <see cref="Error"/> contains structured error information.
    /// </summary>
    public bool HasError;

    /// <summary>
    /// Holds error classification when <see cref="HasError"/> is true.
    /// </summary>
    public WebSocketError Error = new WebSocketError();
}
