namespace sip;

/// <summary>Describes one transport send operation.</summary>
public class SipSendRequest
{
    /// <summary>The message to serialize and send.</summary>
    public SipMessage? Message;
    /// <summary>Pre-serialized bytes to send instead of a message.</summary>
    public byte[] Bytes = new byte[0];
    /// <summary>The destination endpoint.</summary>
    public SipEndpointAddress? RemoteAddress;
    /// <summary>The preferred local endpoint.</summary>
    public SipEndpointAddress? LocalAddress;
    /// <summary>The required transport.</summary>
    public SipTransportKind Transport;
    /// <summary>The preferred reliable connection identifier.</summary>
    public string? ConnectionId;
    /// <summary>The preferred outbound flow identifier.</summary>
    public string? FlowId;
    /// <summary>The monotonic deadline in milliseconds, or negative one when absent.</summary>
    public long DeadlineMilliseconds = -1;
    /// <summary>The cooperative cancellation request.</summary>
    public SipCancellation? Cancellation;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>The requested application-provided transport content coding.</summary>
    public SipContentCodingMetadata? ContentCoding;
}
