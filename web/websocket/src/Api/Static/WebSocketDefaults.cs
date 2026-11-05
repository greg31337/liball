namespace websocket;

/// <summary>
/// Provides finite default resource and timing limits for WebSocket operations.
/// </summary>
public static class WebSocketDefaults
{
    /// <summary>
    /// The default maximum total HTTP header bytes.
    /// </summary>
    public const int MaximumHttpHeaderBytes = 16384;

    /// <summary>
    /// The default maximum number of HTTP header fields.
    /// </summary>
    public const int MaximumHttpHeaderCount = 100;

    /// <summary>
    /// The default maximum HTTP start-line or header-line length.
    /// </summary>
    public const int MaximumHttpLineLength = 8192;

    /// <summary>
    /// The default maximum frame payload length in bytes.
    /// </summary>
    public const int MaximumFramePayloadLength = 16777216;

    /// <summary>
    /// The default maximum application message payload length in bytes.
    /// </summary>
    public const int MaximumMessagePayloadLength = 67108864;

    /// <summary>
    /// The default maximum decompressed application message length in bytes.
    /// </summary>
    public const int MaximumDecompressedMessagePayloadLength = 67108864;

    /// <summary>
    /// The default maximum number of fragments in one message.
    /// </summary>
    public const int MaximumFragmentCount = 1024;

    /// <summary>
    /// The default maximum buffered input bytes.
    /// </summary>
    public const int MaximumBufferedInputBytes = 1048576;

    /// <summary>
    /// The default maximum buffered output bytes.
    /// </summary>
    public const int MaximumBufferedOutputBytes = 1048576;

    /// <summary>
    /// The default maximum number of queued connection events.
    /// </summary>
    public const int MaximumQueuedEvents = 256;

    /// <summary>
    /// The default automatic outbound frame payload length in bytes.
    /// </summary>
    public const int AutomaticFramePayloadLength = 65536;

    /// <summary>
    /// The default close-handshake timeout in milliseconds.
    /// </summary>
    public const int CloseTimeoutMilliseconds = 10000;

    /// <summary>
    /// The default idle interval before a heartbeat Ping in milliseconds.
    /// </summary>
    public const int HeartbeatIdleIntervalMilliseconds = 30000;

    /// <summary>
    /// The default time to await a heartbeat Pong in milliseconds.
    /// </summary>
    public const int HeartbeatPongTimeoutMilliseconds = 10000;

    /// <summary>
    /// The default number of unpredictable bytes in a heartbeat token.
    /// </summary>
    public const int HeartbeatTokenLength = 8;
}
