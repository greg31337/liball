namespace websocket;

/// <summary>
/// Represents an operation that is invalid for the current connection or streaming state.
/// </summary>
public class WebSocketStateException : System.Exception
{
    /// <summary>
    /// Initializes a state exception with its stable diagnostic message.
    /// </summary>
    public WebSocketStateException()
        : base("The WebSocket operation is invalid in the current state.")
    {
    }
}
