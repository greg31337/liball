using System;

namespace http;

/// <summary>
/// Represents an incremental HTTP/2 frame parse or serialization result.
/// </summary>
public class Http2FrameResult
{
    /// <summary>The operation status.</summary>
    public HttpOperationStatus Status;
    /// <summary>The number of input octets consumed.</summary>
    public int Consumed;
    /// <summary>The number of output octets written.</summary>
    public int Written;
    /// <summary>The exact output size required for a complete bounded write.</summary>
    public int RequiredSize;
    /// <summary>The decoded or encoded frame, or <c>null</c> when unavailable.</summary>
    public Http2Frame? Frame;
    /// <summary>The owned serialized frame bytes.</summary>
    public byte[] Bytes = Array.Empty<byte>();
    /// <summary>Indicates that an input-relative error position is available.</summary>
    public bool HasErrorPosition;
    /// <summary>The input-relative error position.</summary>
    public int ErrorPosition;
    /// <summary>A stable error identifier, or <c>null</c> when no error occurred.</summary>
    public string? ErrorCode;
    /// <summary>A caller-readable error explanation, or <c>null</c> when no error occurred.</summary>
    public string? ErrorMessage;
}
