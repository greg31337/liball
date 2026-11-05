using System;

namespace http;

/// <summary>
/// Represents inbound bytes or a decoded frame and an optional outbound connection operation.
/// </summary>
public class Http2ConnectionInput
{
    /// <summary>Indicates that inbound transport bytes are available.</summary>
    public bool HasBytes;
    /// <summary>The inbound transport bytes.</summary>
    public byte[] Bytes = Array.Empty<byte>();
    /// <summary>The decoded inbound frame, or <c>null</c> when bytes require decoding.</summary>
    public Http2Frame? Frame;
    /// <summary>The requested outbound action, or <c>null</c> for inbound-only processing.</summary>
    public Http2Action? Action;
    /// <summary>The request supplied for an outbound message operation, or <c>null</c>.</summary>
    public HttpRequest? Request;
    /// <summary>The response supplied for an outbound message operation, or <c>null</c>.</summary>
    public HttpResponse? Response;
    /// <summary>Indicates that a stream identifier applies to the operation.</summary>
    public bool HasStreamId;
    /// <summary>The stream identifier associated with an outbound operation.</summary>
    public int StreamId;
    /// <summary>Indicates that the outbound operation ends the stream.</summary>
    public bool EndStream;
    /// <summary>Indicates that no more inbound transport bytes will arrive.</summary>
    public bool EndOfInput;
}
