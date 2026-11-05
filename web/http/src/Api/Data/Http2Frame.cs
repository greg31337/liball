using System;
using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents registered or extension HTTP/2 frame type metadata.
/// </summary>
public class Http2Frame
{
    /// <summary>
    /// The declared payload length in octets.
    /// </summary>
    public int Length;

    /// <summary>
    /// The recognized frame type, or <see cref="Http2FrameType.Unknown"/>.
    /// </summary>
    public Http2FrameType FrameType;

    /// <summary>
    /// The exact unsigned eight-bit frame type code from the wire.
    /// </summary>
    public byte TypeCode;

    /// <summary>
    /// Indicates whether <see cref="TypeCode"/> maps to a recognized <see cref="FrameType"/>.
    /// </summary>
    public bool IsKnownType;

    /// <summary>
    /// The exact frame flags octet, including unknown flags.
    /// </summary>
    public byte Flags;

    /// <summary>
    /// Indicates that the reserved stream-identifier bit was set on received input.
    /// </summary>
    public bool ReservedBit;

    /// <summary>
    /// The 31-bit stream identifier.
    /// </summary>
    public int StreamId;

    /// <summary>
    /// The complete raw frame payload.
    /// </summary>
    public byte[] Payload = Array.Empty<byte>();

    /// <summary>
    /// The DATA payload after removing padding.
    /// </summary>
    public byte[] Data = Array.Empty<byte>();

    /// <summary>
    /// The header-block fragment carried by HEADERS, PUSH_PROMISE, or CONTINUATION.
    /// </summary>
    public byte[] HeaderBlockFragment = Array.Empty<byte>();

    /// <summary>
    /// Indicates that END_STREAM is set.
    /// </summary>
    public bool EndStream;

    /// <summary>
    /// Indicates that END_HEADERS is set.
    /// </summary>
    public bool EndHeaders;

    /// <summary>
    /// Indicates that PADDED is set.
    /// </summary>
    public bool IsPadded;

    /// <summary>
    /// The number of trailing padding octets.
    /// </summary>
    public int PaddingLength;

    /// <summary>
    /// The payload length counted against flow control.
    /// </summary>
    public int FlowControlledLength;

    /// <summary>
    /// Indicates that priority fields are present.
    /// </summary>
    public bool HasPriority;

    /// <summary>
    /// Indicates exclusive stream dependency.
    /// </summary>
    public bool ExclusiveDependency;

    /// <summary>
    /// The 31-bit stream dependency identifier.
    /// </summary>
    public int StreamDependency;

    /// <summary>
    /// The decoded priority weight from 1 through 256.
    /// </summary>
    public int Weight;

    /// <summary>
    /// Indicates that negotiated policy requires the deprecated priority signal to be ignored.
    /// </summary>
    public bool PriorityIgnored;

    /// <summary>
    /// Indicates that an error code is present.
    /// </summary>
    public bool HasErrorCode;

    /// <summary>
    /// The protocol scope of the carried error code.
    /// </summary>
    public Http2ErrorScope ErrorScope;

    /// <summary>
    /// The recognized error code, or <see cref="Http2ErrorCode.Unknown"/>.
    /// </summary>
    public Http2ErrorCode ErrorCode;

    /// <summary>
    /// The exact unsigned 32-bit error code.
    /// </summary>
    public uint ErrorCodeValue;

    /// <summary>
    /// Indicates that <see cref="ErrorCodeValue"/> maps to <see cref="ErrorCode"/>.
    /// </summary>
    public bool IsKnownErrorCode;

    /// <summary>
    /// The SETTINGS payload, or <c>null</c> for another frame type.
    /// </summary>
    public Http2Settings? Settings;

    /// <summary>
    /// Indicates that ACK is set.
    /// </summary>
    public bool Acknowledgement;

    /// <summary>
    /// Indicates that a promised stream identifier is present.
    /// </summary>
    public bool HasPromisedStreamId;

    /// <summary>
    /// The promised stream identifier.
    /// </summary>
    public int PromisedStreamId;

    /// <summary>
    /// Indicates that the reserved bit was set on the received promised stream identifier.
    /// </summary>
    public bool PromisedStreamReservedBit;

    /// <summary>
    /// The exact eight-octet PING data.
    /// </summary>
    public byte[] OpaqueData = Array.Empty<byte>();

    /// <summary>
    /// Indicates that a GOAWAY last-stream identifier is present.
    /// </summary>
    public bool HasLastStreamId;

    /// <summary>
    /// The GOAWAY last-stream identifier.
    /// </summary>
    public int LastStreamId;

    /// <summary>
    /// Indicates that the reserved bit was set on the received GOAWAY last-stream identifier.
    /// </summary>
    public bool LastStreamReservedBit;

    /// <summary>
    /// The opaque GOAWAY debug data.
    /// </summary>
    public byte[] DebugData = Array.Empty<byte>();

    /// <summary>
    /// Indicates that a WINDOW_UPDATE increment is present.
    /// </summary>
    public bool HasWindowIncrement;

    /// <summary>
    /// The 31-bit WINDOW_UPDATE increment.
    /// </summary>
    public int WindowIncrement;

    /// <summary>
    /// Indicates that the reserved bit was set on the received WINDOW_UPDATE increment.
    /// </summary>
    public bool WindowIncrementReservedBit;

    /// <summary>
    /// Indicates that a PRIORITY_UPDATE prioritized-element identifier is present.
    /// </summary>
    public bool HasPrioritizedElementId;

    /// <summary>
    /// The PRIORITY_UPDATE prioritized-element identifier.
    /// </summary>
    public int PrioritizedElementId;

    /// <summary>
    /// Indicates that the reserved bit was set on the received prioritized-element identifier.
    /// </summary>
    public bool PrioritizedElementReservedBit;

    /// <summary>
    /// Indicates that the prioritized element is a pushed stream rather than a request stream.
    /// </summary>
    public bool PrioritizedElementIsPush;

    /// <summary>
    /// The ASCII Priority field value carried by PRIORITY_UPDATE.
    /// </summary>
    public byte[] PriorityFieldValue = Array.Empty<byte>();

    /// <summary>
    /// The parsed extensible priority, or <c>null</c> when no valid priority value is available.
    /// </summary>
    public HttpPriority? Priority;

    /// <summary>
    /// The ordered ASCII origins carried by an ORIGIN frame.
    /// </summary>
    public List<string> Origins = new List<string>();

    /// <summary>
    /// Indicates that ORIGIN stream or flag rules require the whole frame to be ignored.
    /// </summary>
    public bool OriginIgnored;
}
