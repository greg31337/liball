namespace websocket;

/// <summary>
/// Identifies which endpoint role a WebSocket protocol component performs.
/// </summary>
public enum WebSocketRole
{
    /// <summary>
    /// The endpoint initiates the opening handshake and masks frames it sends.
    /// </summary>
    Client = 0,

    /// <summary>
    /// The endpoint accepts the opening handshake and receives masked frames.
    /// </summary>
    Server = 1
}
