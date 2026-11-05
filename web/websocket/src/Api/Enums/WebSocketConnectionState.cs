namespace websocket;

/// <summary>
/// Identifies the lifecycle state of a WebSocket connection.
/// </summary>
public enum WebSocketConnectionState
{
    /// <summary>
    /// The opening handshake has not completed.
    /// </summary>
    Connecting = 0,

    /// <summary>
    /// The opening handshake completed successfully.
    /// </summary>
    Open = 1,

    /// <summary>
    /// A closing handshake is in progress.
    /// </summary>
    Closing = 2,

    /// <summary>
    /// The connection ended normally or after transport termination.
    /// </summary>
    Closed = 3,

    /// <summary>
    /// The connection encountered a terminal failure.
    /// </summary>
    Failed = 4
}
