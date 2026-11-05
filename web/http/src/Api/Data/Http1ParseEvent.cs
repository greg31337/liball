using System;

namespace http;

/// <summary>
/// Represents message-context metadata emitted by HTTP/1 parsing.
/// </summary>
public class Http1ParseEvent
{
    /// <summary>
    /// The kind of parser event.
    /// </summary>
    public Http1ParseEventKind EventKind;

    /// <summary>
    /// The semantic kind of message associated with the event.
    /// </summary>
    public HttpMessageKind MessageKind;

    /// <summary>
    /// The parsed request head, or <c>null</c> when the event is not for a request head.
    /// </summary>
    public HttpRequest? Request;

    /// <summary>
    /// The parsed response head, or <c>null</c> when the event is not for a response head.
    /// </summary>
    public HttpResponse? Response;

    /// <summary>
    /// The owned content or tunnel bytes carried by this event.
    /// </summary>
    public byte[] Content = Array.Empty<byte>();

    /// <summary>
    /// The parsed trailer section, or <c>null</c> when the event does not carry trailers.
    /// </summary>
    public HttpHeaders? Trailers;

    /// <summary>
    /// The selected framing metadata, or <c>null</c> before framing is known.
    /// </summary>
    public Http1FramingDecision? FramingDecision;

    /// <summary>
    /// The connection disposition, or <c>null</c> before it is known.
    /// </summary>
    public Http1ConnectionDecision? ConnectionDecision;

    /// <summary>
    /// The selected upgrade protocol token, or <c>null</c> when no protocol switch occurred.
    /// </summary>
    public string? ProtocolName;
}
