using System;
using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents HTTP message mapping and sequencing state for one HTTP/2 stream.
/// </summary>
public class Http2MessageResult
{
    /// <summary>The operation status.</summary>
    public HttpOperationStatus Status;
    /// <summary>The associated stream identifier.</summary>
    public int StreamId;
    /// <summary>The semantic kind of message or field section.</summary>
    public HttpMessageKind MessageKind;
    /// <summary>The decoded request, or <c>null</c> when not applicable.</summary>
    public HttpRequest? Request;
    /// <summary>The decoded response, or <c>null</c> when not applicable.</summary>
    public HttpResponse? Response;
    /// <summary>The ordered decoded or encoded header block.</summary>
    public HttpHeaders Headers = new HttpHeaders();
    /// <summary>The ordered trailing fields, or <c>null</c> before trailers are available.</summary>
    public HttpHeaders? Trailers;
    /// <summary>The DATA payload processed by this operation.</summary>
    public byte[] Content = Array.Empty<byte>();
    /// <summary>Indicates that END_STREAM was received or will be emitted.</summary>
    public bool EndStream;
    /// <summary>Indicates that the complete HTTP message has been mapped.</summary>
    public bool IsComplete;
    /// <summary>Indicates that protocol conversion cannot preserve an exact wire representation.</summary>
    public bool IsLossyConversion;
    /// <summary>Ordered stable notes describing synthesized or unrepresentable conversion details.</summary>
    public List<string> ConversionNotes = new List<string>();
    /// <summary>Indicates that the stream carries a CONNECT tunnel.</summary>
    public bool IsTunnel;
    /// <summary>Indicates that this message belongs to a server-pushed stream.</summary>
    public bool IsPushed;
    /// <summary>Indicates that an associated request stream identifier is available.</summary>
    public bool HasAssociatedStreamId;
    /// <summary>The request stream with which this pushed message is associated.</summary>
    public int AssociatedStreamId;
    /// <summary>The validated request that identifies the pushed response, or <c>null</c>.</summary>
    public HttpRequest? PushRequest;
    /// <summary>Indicates that the promised request method permits storing its response.</summary>
    public bool IsPushCacheable;
    /// <summary>Indicates that the field section or message mapping is malformed.</summary>
    public bool IsMalformed;
    /// <summary>The protocol scope of a mapping error.</summary>
    public Http2ErrorScope ErrorScope;
    /// <summary>Indicates that an HTTP/2 error code applies.</summary>
    public bool HasErrorCode;
    /// <summary>The recognized HTTP/2 error code.</summary>
    public Http2ErrorCode ErrorCode;
    /// <summary>The exact unsigned HTTP/2 error code.</summary>
    public uint ErrorCodeValue;
    /// <summary>Indicates that <see cref="ErrorCodeValue"/> maps to <see cref="ErrorCode"/>.</summary>
    public bool IsKnownErrorCode;
    /// <summary>A stable malformed-message identifier, or <c>null</c> when none applies.</summary>
    public string? ErrorCodeName;
    /// <summary>A caller-readable mapping explanation, or <c>null</c> when omitted.</summary>
    public string? ErrorMessage;
}
