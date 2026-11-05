namespace websocket;

/// <summary>
/// Holds one complete application message send request.
/// </summary>
/// <remarks>
/// The caller owns <see cref="Payload"/> and must not mutate its selected range until the send operation returns.
/// </remarks>
public class WebSocketSendRequest
{
    /// <summary>
    /// Identifies the application message type.
    /// </summary>
    public WebSocketMessageType MessageType;

    /// <summary>
    /// Holds caller-owned message payload bytes.
    /// </summary>
    public byte[] Payload = [];

    /// <summary>
    /// Holds the zero-based start of message bytes in <see cref="Payload"/>.
    /// </summary>
    public int PayloadOffset;

    /// <summary>
    /// Holds the number of message bytes beginning at <see cref="PayloadOffset"/>.
    /// </summary>
    public int PayloadCount;

    /// <summary>
    /// Indicates that negotiated per-message compression should be used when eligible.
    /// </summary>
    public bool Compress;

    /// <summary>
    /// Indicates whether <see cref="FramePayloadLength"/> overrides the connection's automatic frame size.
    /// </summary>
    public bool HasFramePayloadLength;

    /// <summary>
    /// Holds the automatic fragmentation payload length when <see cref="HasFramePayloadLength"/> is true.
    /// </summary>
    public int FramePayloadLength;
}
