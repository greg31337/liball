using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents the selected body framing for an HTTP/1 message.
/// </summary>
public class Http1FramingDecision
{
    /// <summary>
    /// The selected or rejected body framing.
    /// </summary>
    public Http1BodyFraming BodyFraming;

    /// <summary>
    /// Indicates that the framing metadata is valid and unambiguous.
    /// </summary>
    public bool IsValid;

    /// <summary>
    /// Indicates that the message semantics prohibit a message body.
    /// </summary>
    public bool HasNoBody;

    /// <summary>
    /// Indicates that the connection becomes an opaque tunnel after the head.
    /// </summary>
    public bool IsTunnel;

    /// <summary>
    /// Indicates that the body has a boundary independent of connection closure.
    /// </summary>
    public bool IsSelfDelimited;

    /// <summary>
    /// Indicates that a validated declared content length is available.
    /// </summary>
    public bool HasDeclaredLength;

    /// <summary>
    /// The validated declared content length.
    /// </summary>
    public long DeclaredLength;

    /// <summary>
    /// The transfer codings in their application order.
    /// </summary>
    public List<string> TransferCodings = new List<string>();

    /// <summary>
    /// A caller-readable validation explanation, or <c>null</c> when omitted.
    /// </summary>
    public string? Reason;
}
