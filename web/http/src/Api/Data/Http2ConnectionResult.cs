using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents one transport-independent HTTP/2 connection processing result.
/// </summary>
public class Http2ConnectionResult
{
    /// <summary>The operation status.</summary>
    public HttpOperationStatus Status;
    /// <summary>The number of inbound transport octets consumed.</summary>
    public int Consumed;
    /// <summary>The input operation associated with the result, or <c>null</c> when omitted.</summary>
    public Http2ConnectionInput? Input;
    /// <summary>The ordered application and protocol events.</summary>
    public List<Http2Event> Events = new List<Http2Event>();
    /// <summary>The ordered protocol actions for the caller to perform.</summary>
    public List<Http2Action> Actions = new List<Http2Action>();
    /// <summary>An immutable-by-copy connection snapshot, or <c>null</c> when not requested.</summary>
    public Http2ConnectionSnapshot? Snapshot;
    /// <summary>Indicates that the connection is ready to process messages.</summary>
    public bool IsReady;
    /// <summary>Indicates that the connection has entered a terminal state.</summary>
    public bool IsTerminal;
    /// <summary>Indicates that an HTTP/1 message was detected where the HTTP/2 preface was required.</summary>
    public bool UnexpectedHttp1;
    /// <summary>Indicates that the unexpected HTTP/1 version was identified.</summary>
    public bool HasUnexpectedHttpVersion;
    /// <summary>The identified unexpected HTTP/1 protocol version.</summary>
    public HttpProtocolVersion UnexpectedHttpVersion;
    /// <summary>The scope of a reported protocol error.</summary>
    public Http2ErrorScope ErrorScope;
    /// <summary>Indicates that an HTTP/2 error code applies.</summary>
    public bool HasErrorCode;
    /// <summary>The recognized HTTP/2 error code.</summary>
    public Http2ErrorCode ErrorCode;
    /// <summary>The exact unsigned HTTP/2 error code.</summary>
    public uint ErrorCodeValue;
    /// <summary>Indicates that <see cref="ErrorCodeValue"/> maps to <see cref="ErrorCode"/>.</summary>
    public bool IsKnownErrorCode;
    /// <summary>A stable error identifier, or <c>null</c> when no error occurred.</summary>
    public string? ErrorCodeName;
    /// <summary>A caller-readable error explanation, or <c>null</c> when no error occurred.</summary>
    public string? ErrorMessage;
}
