namespace websocket;

/// <summary>
/// Evaluates server opening-handshake requests and constructs the corresponding responses.
/// </summary>
public class WebSocketServerHandshake : IWebSocketServerHandshake
{
    internal Http1ServerHandshakeState Http1State;
    internal ExtendedConnectServerHandshakeState ExtendedState;

    /// <summary>
    /// Initializes an empty server opening-handshake evaluator.
    /// </summary>
    public WebSocketServerHandshake()
    {
        Http1State = new Http1ServerHandshakeState();
        ExtendedState = new ExtendedConnectServerHandshakeState();
    }

    /// <summary>
    /// Evaluates an HTTP/1 Upgrade request without creating connection framing state.
    /// </summary>
    /// <param name="request">The parsed client request.</param>
    /// <param name="options">The server handshake options.</param>
    /// <returns>The complete handshake result and safe HTTP/1 response metadata.</returns>
    /// <exception cref="WebSocketHandshakeException">Thrown when the server options cannot safely evaluate HTTP/1.</exception>
    public WebSocketHandshakeResult EvaluateHttp1Request(
        WebSocketHttpRequest request,
        WebSocketServerHandshakeOptions options)
    {
        return Http1State.EvaluateRequest(request, options);
    }

    /// <summary>
    /// Evaluates an HTTP/2 or HTTP/3 Extended CONNECT request.
    /// </summary>
    /// <param name="request">The client request metadata.</param>
    /// <param name="options">The server handshake options.</param>
    /// <returns>The complete handshake evaluation result.</returns>
    /// <exception cref="WebSocketHandshakeException">Thrown when the options or request transport are invalid.</exception>
    public WebSocketHandshakeResult EvaluateExtendedConnectRequest(
        WebSocketHttpRequest request,
        WebSocketServerHandshakeOptions options)
    {
        if (request != null &&
            request.Transport != WebSocketHandshakeTransport.Http2ExtendedConnect &&
            request.Transport != WebSocketHandshakeTransport.Http3ExtendedConnect)
        {
            throw new WebSocketHandshakeException();
        }

        return ExtendedState.EvaluateRequest(request, options);
    }

    /// <summary>
    /// Creates an independent server connection-option snapshot from an accepted handshake.
    /// </summary>
    /// <param name="handshake">The accepted server handshake result.</param>
    /// <param name="template">The caller-selected connection behavior, collaborators, and limits.</param>
    /// <returns>The frozen server connection options.</returns>
    /// <exception cref="WebSocketStateException">Thrown when the handshake or connection template is not valid for transfer.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when a copied connection limit is unsafe.</exception>
    /// <exception cref="WebSocketExtensionException">Thrown when copied compression settings are invalid.</exception>
    public WebSocketConnectionOptions CreateServerConnectionOptions(
        WebSocketHandshakeResult handshake,
        WebSocketConnectionOptions template)
    {
        return HandshakeConnectionOptionsFactory.CreateOptions(
            WebSocketRole.Server,
            handshake,
            template);
    }
}
