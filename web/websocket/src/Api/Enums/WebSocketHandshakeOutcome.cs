namespace websocket;

/// <summary>
/// Identifies the outcome of opening-handshake processing.
/// </summary>
public enum WebSocketHandshakeOutcome
{
    /// <summary>
    /// The opening handshake was accepted.
    /// </summary>
    Accepted = 0,

    /// <summary>
    /// The peer or application rejected the opening handshake.
    /// </summary>
    Rejected = 1,

    /// <summary>
    /// More input is required to determine the outcome.
    /// </summary>
    Incomplete = 2,

    /// <summary>
    /// The opening handshake is malformed or otherwise failed.
    /// </summary>
    Failed = 3
}
