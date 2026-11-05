namespace websocket;

/// <summary>
/// Holds one ordered WebSocket extension offer or configuration.
/// </summary>
/// <remarks>
/// Parameter order and duplicates are preserved for validation by the responsible extension.
/// </remarks>
public class WebSocketExtensionOffer
{
    /// <summary>
    /// Holds the extension token.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Holds extension parameters in received or preference order.
    /// </summary>
    public WebSocketExtensionParameter[] Parameters = [];
}
