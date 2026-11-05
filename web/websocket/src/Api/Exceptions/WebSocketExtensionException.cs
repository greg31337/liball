namespace websocket;

/// <summary>
/// Represents invalid WebSocket extension configuration or transformation.
/// </summary>
public class WebSocketExtensionException : System.Exception
{
    /// <summary>
    /// Initializes an extension exception with its stable diagnostic message.
    /// </summary>
    public WebSocketExtensionException()
        : base("The WebSocket extension operation failed.")
    {
    }
}
