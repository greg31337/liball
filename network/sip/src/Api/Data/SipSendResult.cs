namespace sip;

/// <summary>Holds the outcome of a transport send operation.</summary>
public class SipSendResult
{
    /// <summary>Indicates that the message was accepted for sending.</summary>
    public bool Success;
    /// <summary>The number of bytes accepted.</summary>
    public int BytesSent;
    /// <summary>The connection used for the operation.</summary>
    public SipConnectionInfo? Connection;
    /// <summary>The flow used for the operation.</summary>
    public SipFlow? Flow;
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>The applied transport content coding and compartment metadata.</summary>
    public SipContentCodingMetadata? ContentCoding;
}
