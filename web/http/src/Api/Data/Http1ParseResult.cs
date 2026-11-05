using System;
using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents the completion status of an incremental HTTP/1 parse operation.
/// </summary>
public class Http1ParseResult
{
    /// <summary>
    /// The operation status.
    /// </summary>
    public HttpOperationStatus Status;

    /// <summary>
    /// The number of input octets consumed by this operation.
    /// </summary>
    public int Consumed;

    /// <summary>
    /// An owned copy of the input octets not consumed by this operation.
    /// </summary>
    public byte[] Unconsumed = Array.Empty<byte>();

    /// <summary>
    /// The ordered events emitted by the parse operation.
    /// </summary>
    public List<Http1ParseEvent> Events = new List<Http1ParseEvent>();

    /// <summary>
    /// The parser options associated with the operation, or <c>null</c> when not reported.
    /// </summary>
    public Http1ParserOptions? Options;

    /// <summary>
    /// Indicates that end-of-input must be signaled to complete a close-delimited response.
    /// </summary>
    public bool NeedsEndOfInput;

    /// <summary>
    /// Indicates that the parser can accept more HTTP input.
    /// </summary>
    public bool CanAcceptInput = true;

    /// <summary>
    /// Indicates that the parser has entered a terminal protocol, tunnel, or failure state.
    /// </summary>
    public bool IsTerminal;

    /// <summary>
    /// Indicates that an input-relative error position is available.
    /// </summary>
    public bool HasErrorPosition;

    /// <summary>
    /// The input-relative position at which malformed data was detected.
    /// </summary>
    public int ErrorPosition;

    /// <summary>
    /// A stable error identifier, or <c>null</c> when no error occurred.
    /// </summary>
    public string? ErrorCode;

    /// <summary>
    /// A caller-readable error explanation, or <c>null</c> when no error occurred.
    /// </summary>
    public string? ErrorMessage;
}
