namespace websocket;

/// <summary>
/// Holds all decoded or requested WebSocket frame header fields.
/// </summary>
public class WebSocketFrameHeader
{
    /// <summary>
    /// Indicates that this is the final frame of a message.
    /// </summary>
    public bool Final;

    /// <summary>
    /// Holds the RSV1 bit.
    /// </summary>
    public bool Rsv1;

    /// <summary>
    /// Holds the RSV2 bit.
    /// </summary>
    public bool Rsv2;

    /// <summary>
    /// Holds the RSV3 bit.
    /// </summary>
    public bool Rsv3;

    /// <summary>
    /// Identifies the frame operation encoded in the four-bit opcode field.
    /// </summary>
    public WebSocketOpcode Opcode;

    /// <summary>
    /// Indicates that the frame payload is masked.
    /// </summary>
    public bool Masked;

    /// <summary>
    /// Holds the four-byte masking key in network byte order when <see cref="Masked"/> is true.
    /// </summary>
    public uint MaskingKey;

    /// <summary>
    /// Holds the signed payload length; legal wire values range from zero through <see cref="WebSocketProtocolConstants.PayloadLengthMaximum"/>.
    /// </summary>
    public long PayloadLength;
}
