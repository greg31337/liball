using System;

namespace http;

/// <summary>
/// Represents an allocating or bounded HTTP/1 serialization result.
/// </summary>
public class Http1SerializationResult
{
    /// <summary>
    /// The operation status.
    /// </summary>
    public HttpOperationStatus Status;

    /// <summary>
    /// The complete owned serialization output, or an empty array for a bounded write.
    /// </summary>
    public byte[] Bytes = Array.Empty<byte>();

    /// <summary>
    /// The number of bytes written to caller-provided output.
    /// </summary>
    public int BytesWritten;

    /// <summary>
    /// The exact output size required for the complete message.
    /// </summary>
    public int RequiredSize;

    /// <summary>
    /// The options used for serialization, or <c>null</c> when not reported.
    /// </summary>
    public Http1SerializationOptions? Options;

    /// <summary>
    /// The selected framing decision, or <c>null</c> when validation failed first.
    /// </summary>
    public Http1FramingDecision? FramingDecision;

    /// <summary>
    /// The resulting connection disposition, or <c>null</c> when unavailable.
    /// </summary>
    public Http1ConnectionDecision? ConnectionDecision;

    /// <summary>
    /// A stable error identifier, or <c>null</c> when serialization succeeded.
    /// </summary>
    public string? ErrorCode;

    /// <summary>
    /// A caller-readable result explanation, or <c>null</c> when omitted.
    /// </summary>
    public string? ErrorMessage;
}
