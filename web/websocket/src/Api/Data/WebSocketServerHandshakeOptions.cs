namespace websocket;

/// <summary>
/// Holds configuration for evaluating and answering a server opening handshake.
/// </summary>
/// <remarks>
/// Callers own assigned arrays. A handshaker that retains option values must copy their contents.
/// </remarks>
public class WebSocketServerHandshakeOptions
{
    /// <summary>
    /// Holds accepted handshake transports.
    /// </summary>
    public WebSocketHandshakeTransport[] SupportedTransports = [WebSocketHandshakeTransport.Http1Upgrade];

    /// <summary>
    /// Indicates whether the server advertised Extended CONNECT protocol support.
    /// </summary>
    public bool ExtendedConnectEnabled;

    /// <summary>
    /// Indicates whether <see cref="OriginPolicy"/> replaces the default same-origin policy.
    /// </summary>
    public bool HasOriginPolicy;

    /// <summary>
    /// Holds the application origin policy when <see cref="HasOriginPolicy"/> is true.
    /// </summary>
    public IWebSocketOriginPolicy? OriginPolicy;

    /// <summary>
    /// Indicates whether <see cref="SubprotocolSelector"/> replaces client-order selection.
    /// </summary>
    public bool HasSubprotocolSelector;

    /// <summary>
    /// Holds the application subprotocol selector when <see cref="HasSubprotocolSelector"/> is true.
    /// </summary>
    public IWebSocketSubprotocolSelector? SubprotocolSelector;

    /// <summary>
    /// Holds supported subprotocols in server preference order.
    /// </summary>
    public string[] SupportedSubprotocols = [];

    /// <summary>
    /// Holds supported extension configurations in server preference order.
    /// </summary>
    public WebSocketExtensionOffer[] SupportedExtensions = [];

    /// <summary>
    /// Holds extension implementations in server selection preference order.
    /// </summary>
    /// <remarks>
    /// This collection is mutually exclusive with <see cref="SupportedExtensions"/>.
    /// </remarks>
    public IWebSocketExtension[] ExtensionHandlers = [];

    /// <summary>
    /// Holds caller-supplied non-critical HTTP response fields in serialization order.
    /// </summary>
    public WebSocketHeaderField[] ExtraFields = [];

    /// <summary>
    /// Holds the application rejection status used when policy rejects a valid request.
    /// </summary>
    public int RejectionStatusCode = 403;

    /// <summary>
    /// Holds caller-supplied rejection response fields in serialization order.
    /// </summary>
    public WebSocketHeaderField[] RejectionFields = [];

    /// <summary>
    /// Indicates whether <see cref="RejectionBody"/> is present.
    /// </summary>
    public bool HasRejectionBody;

    /// <summary>
    /// Holds application rejection body bytes when <see cref="HasRejectionBody"/> is true.
    /// </summary>
    public byte[] RejectionBody = [];

    /// <summary>
    /// Holds the maximum total HTTP header bytes accepted during request parsing.
    /// </summary>
    public int MaximumHeaderBytes = WebSocketDefaults.MaximumHttpHeaderBytes;

    /// <summary>
    /// Holds the maximum number of HTTP fields accepted during request parsing.
    /// </summary>
    public int MaximumHeaderCount = WebSocketDefaults.MaximumHttpHeaderCount;

    /// <summary>
    /// Holds the maximum HTTP request-line or field-line length.
    /// </summary>
    public int MaximumLineLength = WebSocketDefaults.MaximumHttpLineLength;
}
