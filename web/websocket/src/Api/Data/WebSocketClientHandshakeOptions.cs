namespace websocket;

/// <summary>
/// Holds configuration for constructing and validating a client opening handshake.
/// </summary>
/// <remarks>
/// Callers own assigned arrays. A handshaker that retains option values must copy their contents.
/// </remarks>
public class WebSocketClientHandshakeOptions
{
    /// <summary>
    /// Holds the absolute <c>ws</c> or <c>wss</c> URI.
    /// </summary>
    public string Uri = "";

    /// <summary>
    /// Identifies the requested HTTP handshake transport.
    /// </summary>
    public WebSocketHandshakeTransport Transport;

    /// <summary>
    /// Indicates whether <see cref="Origin"/> is included.
    /// </summary>
    public bool HasOrigin;

    /// <summary>
    /// Holds the serialized Origin value when <see cref="HasOrigin"/> is true.
    /// </summary>
    public string Origin = "";

    /// <summary>
    /// Holds subprotocol offers in client preference order.
    /// </summary>
    public string[] Subprotocols = [];

    /// <summary>
    /// Holds extension offers in client preference order.
    /// </summary>
    public WebSocketExtensionOffer[] Extensions = [];

    /// <summary>
    /// Holds extension implementations that create and validate client negotiation offers.
    /// </summary>
    /// <remarks>
    /// This collection is mutually exclusive with <see cref="Extensions"/>.
    /// </remarks>
    public IWebSocketExtension[] ExtensionHandlers = [];

    /// <summary>
    /// Holds caller-supplied non-critical HTTP request fields in serialization order.
    /// </summary>
    public WebSocketHeaderField[] ExtraFields = [];

    /// <summary>
    /// Indicates whether <see cref="UserAgent"/> is included.
    /// </summary>
    public bool HasUserAgent;

    /// <summary>
    /// Holds the User-Agent value when <see cref="HasUserAgent"/> is true.
    /// </summary>
    public string UserAgent = "";

    /// <summary>
    /// Indicates whether the peer advertised Extended CONNECT protocol support.
    /// </summary>
    public bool PeerEnabledExtendedConnect;

    /// <summary>
    /// Holds the maximum total HTTP header bytes accepted during response parsing.
    /// </summary>
    public int MaximumHeaderBytes = WebSocketDefaults.MaximumHttpHeaderBytes;

    /// <summary>
    /// Holds the maximum number of HTTP fields accepted during response parsing.
    /// </summary>
    public int MaximumHeaderCount = WebSocketDefaults.MaximumHttpHeaderCount;

    /// <summary>
    /// Holds the maximum HTTP status-line or field-line length.
    /// </summary>
    public int MaximumLineLength = WebSocketDefaults.MaximumHttpLineLength;
}
