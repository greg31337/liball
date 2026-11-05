namespace websocket;

/// <summary>
/// Holds the public vocabulary returned by opening-handshake processing.
/// </summary>
public class WebSocketHandshakeResult
{
    /// <summary>
    /// Identifies the opening-handshake outcome.
    /// </summary>
    public WebSocketHandshakeOutcome Outcome = WebSocketHandshakeOutcome.Incomplete;

    /// <summary>
    /// Identifies the HTTP transport used for the opening handshake.
    /// </summary>
    public WebSocketHandshakeTransport Transport;

    /// <summary>
    /// Indicates whether <see cref="Request"/> contains opening-handshake request metadata.
    /// </summary>
    public bool HasRequest;

    /// <summary>
    /// Holds opening-handshake request metadata when <see cref="HasRequest"/> is true.
    /// </summary>
    public WebSocketHttpRequest Request = new WebSocketHttpRequest();

    /// <summary>
    /// Indicates whether <see cref="Response"/> contains opening-handshake response metadata.
    /// </summary>
    public bool HasResponse;

    /// <summary>
    /// Holds opening-handshake response metadata when <see cref="HasResponse"/> is true.
    /// </summary>
    public WebSocketHttpResponse Response = new WebSocketHttpResponse();

    /// <summary>
    /// Indicates whether <see cref="SelectedSubprotocol"/> contains a negotiated subprotocol.
    /// </summary>
    public bool HasSelectedSubprotocol;

    /// <summary>
    /// Holds the negotiated subprotocol when <see cref="HasSelectedSubprotocol"/> is true.
    /// </summary>
    public string SelectedSubprotocol = "";

    /// <summary>
    /// Holds extension negotiation results in negotiation order.
    /// </summary>
    public WebSocketExtensionNegotiation[] Extensions = [];

    /// <summary>
    /// Indicates whether <see cref="ClientKey"/> contains the exact client handshake key text.
    /// </summary>
    public bool HasClientKey;

    /// <summary>
    /// Holds the exact client handshake key text when <see cref="HasClientKey"/> is true.
    /// </summary>
    public string ClientKey = "";

    /// <summary>
    /// Indicates whether <see cref="Error"/> describes a handshake failure.
    /// </summary>
    public bool HasError;

    /// <summary>
    /// Holds error classification when <see cref="HasError"/> is true.
    /// </summary>
    public WebSocketError Error = new WebSocketError();
}
