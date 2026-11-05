namespace websocket;

/// <summary>
/// Represents invalid WebSocket opening-handshake construction or validation.
/// </summary>
public class WebSocketHandshakeException : System.Exception
{
    /// <summary>
    /// Initializes a handshake exception with its stable diagnostic message.
    /// </summary>
    public WebSocketHandshakeException()
        : base("The WebSocket opening handshake is invalid.")
    {
    }
}
