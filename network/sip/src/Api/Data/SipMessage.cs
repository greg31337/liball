namespace sip;

/// <summary>
/// Holds a complete SIP request or response while preserving ordered fields and binary body bytes.
/// </summary>
public class SipMessage
{
    /// <summary>The message kind.</summary>
    public SipMessageKind Kind;

    /// <summary>The recognized request method.</summary>
    public SipMethod Method;

    /// <summary>The original request method token, including an extension method.</summary>
    public string? MethodToken;

    /// <summary>The request URI, or null for a response.</summary>
    public SipUri? RequestUri;

    /// <summary>The response status class.</summary>
    public SipStatusClass StatusClass;

    /// <summary>The response status code, or zero for a request.</summary>
    public int StatusCode;

    /// <summary>The response reason phrase, or null for a request.</summary>
    public string? ReasonPhrase;

    /// <summary>The protocol version token from the start line.</summary>
    public string? ProtocolVersion = SipTransportNames.ProtocolVersion;

    /// <summary>The ordered header fields, including duplicates and unknown fields.</summary>
    public SipHeader[] Headers = new SipHeader[0];

    /// <summary>The parsed Via values in wire order.</summary>
    public SipViaValue[] ViaValues = new SipViaValue[0];

    /// <summary>The parsed Route values in wire order.</summary>
    public SipRouteValue[] RouteValues = new SipRouteValue[0];

    /// <summary>The parsed Record-Route values in wire order.</summary>
    public SipRouteValue[] RecordRouteValues = new SipRouteValue[0];

    /// <summary>The parsed Contact values in wire order.</summary>
    public SipContactValue[] ContactValues = new SipContactValue[0];

    /// <summary>The parsed From name-address.</summary>
    public SipNameAddress? From;

    /// <summary>The parsed To name-address.</summary>
    public SipNameAddress? To;

    /// <summary>The parsed CSeq value.</summary>
    public SipCSeqValue? CSeq;

    /// <summary>The parsed body media type.</summary>
    public SipMediaType? ContentType;

    /// <summary>The body disposition, or null when no disposition is supplied.</summary>
    public SipContentDisposition? ContentDisposition;

    /// <summary>The message body bytes.</summary>
    public byte[] Body = new byte[0];

    /// <summary>The original complete message bytes when preservation was requested.</summary>
    public byte[] OriginalBytes = new byte[0];
}
