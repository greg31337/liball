namespace websocket;

/// <summary>
/// Identifies the HTTP transport used for the WebSocket opening handshake.
/// </summary>
public enum WebSocketHandshakeTransport
{
    /// <summary>
    /// Uses an HTTP/1 Upgrade request and a status 101 response.
    /// </summary>
    Http1Upgrade = 0,

    /// <summary>
    /// Uses HTTP/2 Extended CONNECT.
    /// </summary>
    Http2ExtendedConnect = 1,

    /// <summary>
    /// Uses HTTP/3 Extended CONNECT.
    /// </summary>
    Http3ExtendedConnect = 2
}
