namespace websocket;

/// <summary>
/// Holds the result of one incremental extension transform call.
/// </summary>
/// <remarks>
/// <see cref="Output"/> is owned by the result. <see cref="ProducedCount"/> identifies the valid prefix.
/// </remarks>
public class WebSocketTransformResult
{
    /// <summary>
    /// Identifies whether transformation completed, paused, or failed.
    /// </summary>
    public WebSocketParseStatus Status;

    /// <summary>
    /// Holds the number of source bytes consumed.
    /// </summary>
    public int ConsumedCount;

    /// <summary>
    /// Holds the number of valid bytes in <see cref="Output"/>.
    /// </summary>
    public int ProducedCount;

    /// <summary>
    /// Holds transformed output bytes; only the prefix identified by <see cref="ProducedCount"/> is valid.
    /// </summary>
    public byte[] Output = [];

    /// <summary>
    /// Indicates that the transform sets RSV1 on the first output frame.
    /// </summary>
    public bool SetRsv1;

    /// <summary>
    /// Indicates that the transform sets RSV2 on the first output frame.
    /// </summary>
    public bool SetRsv2;

    /// <summary>
    /// Indicates that the transform sets RSV3 on the first output frame.
    /// </summary>
    public bool SetRsv3;

    /// <summary>
    /// Indicates whether <see cref="Error"/> describes a transform failure.
    /// </summary>
    public bool HasError;

    /// <summary>
    /// Holds structured error information when <see cref="HasError"/> is true.
    /// </summary>
    public WebSocketError Error = new WebSocketError();
}
