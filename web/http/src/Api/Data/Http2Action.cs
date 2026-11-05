using System;

namespace http;

/// <summary>
/// Represents an action requested by HTTP/2 processing.
/// </summary>
public class Http2Action
{
    /// <summary>
    /// The action kind.
    /// </summary>
    public Http2ActionKind ActionKind;

    /// <summary>The raw connection-preface bytes to send.</summary>
    public byte[] Bytes = Array.Empty<byte>();
    /// <summary>The frame to send, or <c>null</c> when the action is higher level.</summary>
    public Http2Frame? Frame;
    /// <summary>The message operation, or <c>null</c> for a frame-level action.</summary>
    public Http2MessageResult? Message;
    /// <summary>The settings to send or acknowledge, or <c>null</c> when not applicable.</summary>
    public Http2Settings? Settings;
    /// <summary>Indicates that a stream identifier applies to the action.</summary>
    public bool HasStreamId;
    /// <summary>The associated stream identifier.</summary>
    public int StreamId;
    /// <summary>The flow-control capacity to release or reserve in octets.</summary>
    public int Capacity;
    /// <summary>Indicates that an error code applies to the action.</summary>
    public bool HasErrorCode;
    /// <summary>The recognized error code, or <see cref="Http2ErrorCode.Unknown"/>.</summary>
    public Http2ErrorCode ErrorCode;
    /// <summary>The exact unsigned error code.</summary>
    public uint ErrorCodeValue;
    /// <summary>Indicates that <see cref="ErrorCodeValue"/> maps to <see cref="ErrorCode"/>.</summary>
    public bool IsKnownErrorCode;
    /// <summary>Indicates that a GOAWAY last-stream identifier applies.</summary>
    public bool HasLastStreamId;
    /// <summary>The GOAWAY last-stream identifier.</summary>
    public int LastStreamId;
    /// <summary>Opaque PING or GOAWAY application data.</summary>
    public byte[] OpaqueData = Array.Empty<byte>();
}
