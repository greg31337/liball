namespace websocket;

/// <summary>
/// Holds message and limit context supplied to an extension transform.
/// </summary>
public class WebSocketExtensionContext
{
    /// <summary>
    /// Identifies the local endpoint role.
    /// </summary>
    public WebSocketRole Role;

    /// <summary>
    /// Indicates an outbound transform when true and an inbound transform when false.
    /// </summary>
    public bool Outbound;

    /// <summary>
    /// Indicates that outbound compression is requested for this message.
    /// </summary>
    public bool RequestCompression;

    /// <summary>
    /// Indicates that RSV1 is set on the inbound frame containing this chunk.
    /// </summary>
    public bool Rsv1;

    /// <summary>
    /// Indicates that RSV2 is set on the inbound frame containing this chunk.
    /// </summary>
    public bool Rsv2;

    /// <summary>
    /// Indicates that RSV3 is set on the inbound frame containing this chunk.
    /// </summary>
    public bool Rsv3;

    /// <summary>
    /// Indicates that <see cref="Opcode"/> identifies the frame opcode associated with this chunk.
    /// </summary>
    public bool HasOpcode;

    /// <summary>
    /// Identifies the frame opcode when <see cref="HasOpcode"/> is true.
    /// </summary>
    public WebSocketOpcode Opcode;

    /// <summary>
    /// Identifies the application message type.
    /// </summary>
    public WebSocketMessageType MessageType;

    /// <summary>
    /// Indicates the first transform chunk of the message.
    /// </summary>
    public bool FirstChunk;

    /// <summary>
    /// Indicates the final transform chunk of the message.
    /// </summary>
    public bool FinalChunk;

    /// <summary>
    /// Indicates the first WebSocket fragment of the message.
    /// </summary>
    public bool FirstFragment;

    /// <summary>
    /// Indicates the final WebSocket fragment of the message.
    /// </summary>
    public bool FinalFragment;

    /// <summary>
    /// Holds the maximum permitted frame payload length in bytes.
    /// </summary>
    public int MaximumFramePayloadLength = WebSocketDefaults.MaximumFramePayloadLength;

    /// <summary>
    /// Holds the maximum permitted application message length in bytes.
    /// </summary>
    public int MaximumMessagePayloadLength = WebSocketDefaults.MaximumMessagePayloadLength;

    /// <summary>
    /// Holds the maximum permitted decompressed message length in bytes.
    /// </summary>
    public int MaximumDecompressedMessagePayloadLength = WebSocketDefaults.MaximumDecompressedMessagePayloadLength;

    /// <summary>
    /// Holds the maximum transform output that may be buffered in bytes.
    /// </summary>
    public int MaximumBufferedOutputBytes = WebSocketDefaults.MaximumBufferedOutputBytes;
}
