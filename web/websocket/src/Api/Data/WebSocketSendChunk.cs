namespace websocket;

/// <summary>
/// Holds one chunk of a streaming application message send.
/// </summary>
/// <remarks>
/// The caller owns <see cref="Payload"/> and must not mutate its selected range until the send operation returns.
/// </remarks>
public class WebSocketSendChunk
{
    /// <summary>
    /// Indicates the first chunk of a streaming message.
    /// </summary>
    public bool First;

    /// <summary>
    /// Indicates the final chunk of a streaming message.
    /// </summary>
    public bool Final;

    /// <summary>
    /// Identifies the application message type; it is read when <see cref="First"/> is true.
    /// </summary>
    public WebSocketMessageType MessageType;

    /// <summary>
    /// Holds caller-owned message chunk bytes.
    /// </summary>
    public byte[] Payload = [];

    /// <summary>
    /// Holds the zero-based start of chunk bytes in <see cref="Payload"/>.
    /// </summary>
    public int PayloadOffset;

    /// <summary>
    /// Holds the number of chunk bytes beginning at <see cref="PayloadOffset"/>.
    /// </summary>
    public int PayloadCount;

    /// <summary>
    /// Indicates that negotiated per-message compression should be used; it is read when <see cref="First"/> is true.
    /// </summary>
    public bool Compress;
}
