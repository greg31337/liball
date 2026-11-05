namespace sip;

/// <summary>Holds a transport-neutral received or sent message envelope.</summary>
public class SipTransportMessage
{
    /// <summary>The parsed SIP message.</summary>
    public SipMessage? Message;
    /// <summary>The exact transported bytes.</summary>
    public byte[] Bytes = new byte[0];
    /// <summary>The local endpoint.</summary>
    public SipEndpointAddress? LocalAddress;
    /// <summary>The remote endpoint.</summary>
    public SipEndpointAddress? RemoteAddress;
    /// <summary>The selected transport.</summary>
    public SipTransportKind Transport;
    /// <summary>The reliable connection metadata.</summary>
    public SipConnectionInfo? Connection;
    /// <summary>The outbound flow metadata.</summary>
    public SipFlow? Flow;
    /// <summary>The parse outcome.</summary>
    public SipParseResult? ParseResult;
    /// <summary>The monotonic receive or send timestamp in milliseconds.</summary>
    public long TimestampMilliseconds = -1;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>The applied transport content coding and compartment metadata.</summary>
    public SipContentCodingMetadata? ContentCoding;
}
