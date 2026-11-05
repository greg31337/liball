namespace websocket;

/// <summary>
/// Holds immutable-on-use configuration for one WebSocket connection.
/// </summary>
/// <remarks>
/// Callers own assigned arrays and collaborators. A connection must copy arrays and scalar values before retaining the options.
/// </remarks>
public class WebSocketConnectionOptions
{
    /// <summary>
    /// Identifies the local endpoint role.
    /// </summary>
    public WebSocketRole Role;

    /// <summary>
    /// Identifies the accepted handshake transport.
    /// </summary>
    public WebSocketHandshakeTransport Transport;

    /// <summary>
    /// Identifies how received application messages are delivered.
    /// </summary>
    public WebSocketMessageDeliveryMode DeliveryMode;

    /// <summary>
    /// Indicates whether <see cref="SelectedSubprotocol"/> contains a negotiated subprotocol.
    /// </summary>
    public bool HasSelectedSubprotocol;

    /// <summary>
    /// Holds the negotiated subprotocol when <see cref="HasSelectedSubprotocol"/> is true.
    /// </summary>
    public string SelectedSubprotocol = "";

    /// <summary>
    /// Holds negotiated extensions in transformation order.
    /// </summary>
    public IWebSocketExtension[] Extensions = [];

    /// <summary>
    /// Holds independent snapshots of negotiated extension configurations in transformation order.
    /// </summary>
    public WebSocketExtensionNegotiation[] NegotiatedExtensions = [];

    /// <summary>
    /// Indicates whether <see cref="PerMessageDeflate"/> supplies per-message compression options.
    /// </summary>
    public bool HasPerMessageDeflate;

    /// <summary>
    /// Holds per-message compression options when <see cref="HasPerMessageDeflate"/> is true.
    /// </summary>
    public PerMessageDeflateOptions PerMessageDeflate = new PerMessageDeflateOptions();

    /// <summary>
    /// Holds the maximum accepted frame payload length in bytes.
    /// </summary>
    public int MaximumFramePayloadLength = WebSocketDefaults.MaximumFramePayloadLength;

    /// <summary>
    /// Holds the maximum accepted application message length in bytes.
    /// </summary>
    public int MaximumMessagePayloadLength = WebSocketDefaults.MaximumMessagePayloadLength;

    /// <summary>
    /// Holds the maximum accepted decompressed message length in bytes.
    /// </summary>
    public int MaximumDecompressedMessagePayloadLength = WebSocketDefaults.MaximumDecompressedMessagePayloadLength;

    /// <summary>
    /// Holds the maximum number of frames in one fragmented message.
    /// </summary>
    public int MaximumFragmentCount = WebSocketDefaults.MaximumFragmentCount;

    /// <summary>
    /// Holds the maximum buffered input bytes.
    /// </summary>
    public int MaximumBufferedInputBytes = WebSocketDefaults.MaximumBufferedInputBytes;

    /// <summary>
    /// Holds the maximum buffered output bytes.
    /// </summary>
    public int MaximumBufferedOutputBytes = WebSocketDefaults.MaximumBufferedOutputBytes;

    /// <summary>
    /// Holds the maximum number of queued connection events.
    /// </summary>
    public int MaximumQueuedEvents = WebSocketDefaults.MaximumQueuedEvents;

    /// <summary>
    /// Holds the automatic outbound data-frame payload length in bytes.
    /// </summary>
    public int AutomaticFramePayloadLength = WebSocketDefaults.AutomaticFramePayloadLength;

    /// <summary>
    /// Indicates that received Ping payloads are echoed automatically in Pong frames.
    /// </summary>
    public bool AutomaticPong = true;

    /// <summary>
    /// Indicates that a valid received Close is answered automatically when necessary.
    /// </summary>
    public bool AutomaticCloseResponse = true;

    /// <summary>
    /// Holds the close-handshake timeout in elapsed milliseconds.
    /// </summary>
    public int CloseTimeoutMilliseconds = WebSocketDefaults.CloseTimeoutMilliseconds;

    /// <summary>
    /// Holds caller-driven heartbeat settings.
    /// </summary>
    public WebSocketHeartbeatOptions Heartbeat = new WebSocketHeartbeatOptions();

    /// <summary>
    /// Indicates whether <see cref="MaskKeySource"/> supplies client frame masking keys.
    /// </summary>
    public bool HasMaskKeySource;

    /// <summary>
    /// Holds the client frame masking key source when <see cref="HasMaskKeySource"/> is true.
    /// </summary>
    public IWebSocketMaskKeySource? MaskKeySource;
}
