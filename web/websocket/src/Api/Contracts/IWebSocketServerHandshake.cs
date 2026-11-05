namespace websocket;

/// <summary>
/// Defines server opening-handshake evaluation operations.
/// </summary>
public interface IWebSocketServerHandshake
{
    /// <summary>
    /// Evaluates an HTTP/1 Upgrade request.
    /// </summary>
    /// <param name="request">The parsed client request.</param>
    /// <param name="options">The server handshake options.</param>
    /// <returns>The handshake evaluation result.</returns>
    WebSocketHandshakeResult EvaluateHttp1Request(WebSocketHttpRequest request, WebSocketServerHandshakeOptions options);

    /// <summary>
    /// Evaluates an HTTP/2 or HTTP/3 Extended CONNECT request.
    /// </summary>
    /// <param name="request">The client request metadata.</param>
    /// <param name="options">The server handshake options.</param>
    /// <returns>The handshake evaluation result.</returns>
    WebSocketHandshakeResult EvaluateExtendedConnectRequest(WebSocketHttpRequest request, WebSocketServerHandshakeOptions options);

    /// <summary>
    /// Creates an independent server connection-option snapshot from an accepted handshake.
    /// </summary>
    /// <param name="handshake">The accepted server handshake result.</param>
    /// <param name="template">The caller-selected connection behavior, collaborators, and limits.</param>
    /// <returns>The frozen server connection options.</returns>
    WebSocketConnectionOptions CreateServerConnectionOptions(
        WebSocketHandshakeResult handshake,
        WebSocketConnectionOptions template);
}
