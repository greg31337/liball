namespace websocket;

/// <summary>
/// Identifies how received application messages are delivered.
/// </summary>
public enum WebSocketMessageDeliveryMode
{
    /// <summary>
    /// Delivers each complete message as one bounded value.
    /// </summary>
    Reassembled = 0,

    /// <summary>
    /// Delivers message boundaries and bounded data chunks.
    /// </summary>
    Streaming = 1
}
