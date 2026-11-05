namespace websocket;

/// <summary>
/// Represents a caller request that exceeds a configured local limit before queuing.
/// </summary>
public class WebSocketLimitException : System.Exception
{
    /// <summary>
    /// Initializes a limit exception with its stable diagnostic message.
    /// </summary>
    public WebSocketLimitException()
        : base("The WebSocket operation exceeds a configured limit.")
    {
    }
}
