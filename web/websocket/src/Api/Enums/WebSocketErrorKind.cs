namespace websocket;

/// <summary>
/// Identifies the category of a WebSocket error.
/// </summary>
public enum WebSocketErrorKind
{
    /// <summary>
    /// The opening handshake failed.
    /// </summary>
    Handshake = 0,

    /// <summary>
    /// WebSocket wire protocol validation failed.
    /// </summary>
    Protocol = 1,

    /// <summary>
    /// UTF-8 validation failed.
    /// </summary>
    Utf8 = 2,

    /// <summary>
    /// Compression negotiation or transformation failed.
    /// </summary>
    Compression = 3,

    /// <summary>
    /// A configured resource limit was exceeded.
    /// </summary>
    Limit = 4,

    /// <summary>
    /// An operation was invalid for the current state.
    /// </summary>
    State = 5,

    /// <summary>
    /// The underlying transport ended or failed.
    /// </summary>
    Transport = 6
}
