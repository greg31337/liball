namespace sip;

/// <summary>Holds one application-provided transport content-coding result.</summary>
public class SipContentCodingResult
{
    /// <summary>Indicates that the coding operation succeeded.</summary>
    public bool Success;
    /// <summary>The detached encoded or decoded bytes.</summary>
    public byte[] Bytes = new byte[0];
    /// <summary>The updated coding and compartment metadata.</summary>
    public SipContentCodingMetadata? Metadata;
    /// <summary>The failure description, or null on success.</summary>
    public string? ErrorMessage;
}
