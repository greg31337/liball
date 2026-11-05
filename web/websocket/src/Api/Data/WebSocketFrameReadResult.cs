namespace websocket;

/// <summary>
/// Holds the public vocabulary returned by incremental frame reading.
/// </summary>
public class WebSocketFrameReadResult
{
    /// <summary>
    /// Identifies whether frame reading completed, paused, ended, or failed.
    /// </summary>
    public WebSocketParseStatus Status = WebSocketParseStatus.NeedInput;

    /// <summary>
    /// Holds the number of source bytes consumed by the read operation.
    /// </summary>
    public int ConsumedCount;

    /// <summary>
    /// Indicates whether <see cref="Header"/> contains a decoded frame header.
    /// </summary>
    public bool HasHeader;

    /// <summary>
    /// Holds the decoded header when enough input was available.
    /// </summary>
    public WebSocketFrameHeader Header = new WebSocketFrameHeader();

    /// <summary>
    /// Holds an unmasked payload chunk owned by this result.
    /// </summary>
    public byte[] Payload = [];

    /// <summary>
    /// Holds the zero-based start of valid payload bytes in <see cref="Payload"/>.
    /// </summary>
    public int PayloadOffset;

    /// <summary>
    /// Holds the number of valid payload bytes beginning at <see cref="PayloadOffset"/>.
    /// </summary>
    public int PayloadCount;

    /// <summary>
    /// Indicates that this result contains the final payload byte of the frame.
    /// </summary>
    public bool EndOfFrame;

    /// <summary>
    /// Indicates whether <see cref="Error"/> describes frame rejection or failure.
    /// </summary>
    public bool HasError;

    /// <summary>
    /// Holds error classification when <see cref="HasError"/> is true.
    /// </summary>
    public WebSocketError Error = new WebSocketError();
}
