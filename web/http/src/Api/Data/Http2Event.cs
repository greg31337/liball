using System;

namespace http;

/// <summary>
/// Represents an event delivered by HTTP/2 processing.
/// </summary>
public class Http2Event
{
    /// <summary>
    /// The event kind.
    /// </summary>
    public Http2EventKind EventKind;

    /// <summary>
    /// The decoded frame associated with the event, or <c>null</c> when no frame applies.
    /// </summary>
    public Http2Frame? Frame;

    /// <summary>
    /// Indicates that a stream identifier applies to the event.
    /// </summary>
    public bool HasStreamId;

    /// <summary>
    /// The associated stream identifier.
    /// </summary>
    public int StreamId;

    /// <summary>
    /// The semantic message kind associated with the event.
    /// </summary>
    public HttpMessageKind MessageKind;

    /// <summary>
    /// The delivered request head, or <c>null</c> when not applicable.
    /// </summary>
    public HttpRequest? Request;

    /// <summary>
    /// The delivered response head, or <c>null</c> when not applicable.
    /// </summary>
    public HttpResponse? Response;

    /// <summary>
    /// The delivered message content bytes.
    /// </summary>
    public byte[] Content = Array.Empty<byte>();

    /// <summary>
    /// The delivered trailing fields, or <c>null</c> when not applicable.
    /// </summary>
    public HttpHeaders? Trailers;

    /// <summary>
    /// The message mapping result, or <c>null</c> for a connection-only event.
    /// </summary>
    public Http2MessageResult? Message;

    /// <summary>
    /// The effective peer settings, or <c>null</c> when settings did not change.
    /// </summary>
    public Http2Settings? Settings;

    /// <summary>
    /// The PING acknowledgement data.
    /// </summary>
    public byte[] PingData = Array.Empty<byte>();

    /// <summary>
    /// Indicates whether the event carries an HTTP/2 error code.
    /// </summary>
    public bool HasErrorCode;

    /// <summary>
    /// The recognized error code, or <see cref="Http2ErrorCode.Unknown"/>.
    /// </summary>
    public Http2ErrorCode ErrorCode;

    /// <summary>
    /// The exact unsigned 32-bit error code from the wire.
    /// </summary>
    public uint ErrorCodeValue;

    /// <summary>
    /// Indicates whether <see cref="ErrorCodeValue"/> maps to a recognized <see cref="ErrorCode"/>.
    /// </summary>
    public bool IsKnownErrorCode;

    /// <summary>
    /// The protocol scope of the error.
    /// </summary>
    public Http2ErrorScope ErrorScope;

    /// <summary>
    /// A caller-readable error explanation, or <c>null</c> when no error occurred.
    /// </summary>
    public string? ErrorMessage;
}
