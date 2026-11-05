namespace websocket;

/// <summary>
/// Defines client opening-handshake construction and validation operations.
/// </summary>
public interface IWebSocketClientHandshake
{
    /// <summary>
    /// Creates an HTTP/1 Upgrade request from client options.
    /// </summary>
    /// <param name="options">The client handshake options.</param>
    /// <returns>The request construction result.</returns>
    WebSocketHandshakeResult CreateHttp1Request(WebSocketClientHandshakeOptions options);

    /// <summary>
    /// Validates an HTTP/1 Upgrade response against retained client request state.
    /// </summary>
    /// <param name="response">The server response metadata.</param>
    /// <returns>The response validation result.</returns>
    WebSocketHandshakeResult ValidateHttp1Response(WebSocketHttpResponse response);

    /// <summary>
    /// Creates an HTTP/2 or HTTP/3 Extended CONNECT request from client options.
    /// </summary>
    /// <param name="options">The client handshake options.</param>
    /// <returns>The request construction result.</returns>
    WebSocketHandshakeResult CreateExtendedConnectRequest(WebSocketClientHandshakeOptions options);

    /// <summary>
    /// Validates an Extended CONNECT response against retained client request state.
    /// </summary>
    /// <param name="response">The server response metadata.</param>
    /// <returns>The response validation result.</returns>
    WebSocketHandshakeResult ValidateExtendedConnectResponse(WebSocketHttpResponse response);

    /// <summary>
    /// Creates an independent client connection-option snapshot from an accepted handshake.
    /// </summary>
    /// <param name="handshake">The accepted client handshake result.</param>
    /// <param name="template">The caller-selected connection behavior, collaborators, and limits.</param>
    /// <returns>The frozen client connection options.</returns>
    WebSocketConnectionOptions CreateClientConnectionOptions(
        WebSocketHandshakeResult handshake,
        WebSocketConnectionOptions template);
}
