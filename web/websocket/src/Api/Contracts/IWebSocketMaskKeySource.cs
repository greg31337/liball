namespace websocket;

/// <summary>
/// Defines generation of fresh WebSocket frame masking keys.
/// </summary>
public interface IWebSocketMaskKeySource
{
    /// <summary>
    /// Creates a fresh unpredictable four-byte masking key.
    /// </summary>
    /// <returns>The masking key interpreted in network byte order.</returns>
    uint CreateMaskingKey();
}
