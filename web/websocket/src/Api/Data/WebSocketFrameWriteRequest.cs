namespace websocket;

/// <summary>
/// Holds one incremental WebSocket frame write request.
/// </summary>
/// <remarks>
/// The caller owns <see cref="Payload"/> and must not mutate its selected range until the write operation returns.
/// </remarks>
public class WebSocketFrameWriteRequest
{
    /// <summary>
    /// Holds the requested frame header fields.
    /// </summary>
    public WebSocketFrameHeader Header = new WebSocketFrameHeader();

    /// <summary>
    /// Holds caller-owned payload bytes.
    /// </summary>
    public byte[] Payload = [];

    /// <summary>
    /// Holds the zero-based start of payload bytes to write.
    /// </summary>
    public int PayloadOffset;

    /// <summary>
    /// Holds the number of payload bytes to write beginning at <see cref="PayloadOffset"/>.
    /// </summary>
    public int PayloadCount;

    /// <summary>
    /// Indicates whether <see cref="MaskKeySource"/> supplies a fresh client masking key.
    /// </summary>
    public bool HasMaskKeySource;

    /// <summary>
    /// Holds the masking key source when <see cref="HasMaskKeySource"/> is true.
    /// </summary>
    public IWebSocketMaskKeySource? MaskKeySource;

    /// <summary>
    /// Holds the payload stream offset used to continue the four-byte masking cycle.
    /// </summary>
    public int MaskingOffset;
}
