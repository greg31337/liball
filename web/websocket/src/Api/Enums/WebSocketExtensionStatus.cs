namespace websocket;

/// <summary>
/// Identifies the negotiation state of a WebSocket extension configuration.
/// </summary>
public enum WebSocketExtensionStatus
{
    /// <summary>
    /// The extension configuration was offered.
    /// </summary>
    Offered = 0,

    /// <summary>
    /// The extension configuration was negotiated.
    /// </summary>
    Negotiated = 1,

    /// <summary>
    /// The extension configuration was declined without failing the handshake.
    /// </summary>
    Declined = 2,

    /// <summary>
    /// The extension configuration is invalid.
    /// </summary>
    Invalid = 3
}
