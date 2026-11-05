namespace sip;

/// <summary>Describes one application-provided transport content-coding operation.</summary>
public class SipContentCodingRequest
{
    /// <summary>The detached input bytes.</summary>
    public byte[] Bytes = new byte[0];
    /// <summary>The coding and compartment metadata.</summary>
    public SipContentCodingMetadata? Metadata;
    /// <summary>The maximum permitted output size in bytes.</summary>
    public int MaxOutputBytes = SipDefaults.MaxMessageBytes;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
