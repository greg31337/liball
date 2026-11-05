namespace websocket;

/// <summary>
/// Identifies the application data type of a WebSocket message.
/// </summary>
public enum WebSocketMessageType
{
    /// <summary>
    /// The message contains UTF-8 text.
    /// </summary>
    Text = 0,

    /// <summary>
    /// The message contains binary data.
    /// </summary>
    Binary = 1
}
