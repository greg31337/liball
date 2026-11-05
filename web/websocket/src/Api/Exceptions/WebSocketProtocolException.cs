namespace websocket;

/// <summary>
/// Represents invalid WebSocket wire protocol data at an exception-based API boundary.
/// </summary>
public class WebSocketProtocolException : System.Exception
{
    /// <summary>
    /// Initializes a protocol exception with its stable diagnostic message.
    /// </summary>
    public WebSocketProtocolException()
        : base("The WebSocket wire protocol is invalid.")
    {
    }
}
