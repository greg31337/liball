namespace rtp;

/// <summary>
/// Contains a signaling-supplied RTP header-extension mapping.
/// </summary>
public class RtpHeaderExtensionMapping
{
    /// <summary>The locally negotiated extension identifier.</summary>
    public int Identifier;

    /// <summary>The extension URI.</summary>
    public string Uri = "";

    /// <summary>Whether the mapping can use the RFC 8285 one-byte format.</summary>
    public bool SupportsOneByte;

    /// <summary>Whether the mapping can use the RFC 8285 two-byte format.</summary>
    public bool SupportsTwoByte;

    /// <summary>The preferred representation when more than one format is available.</summary>
    public RtpHeaderExtensionFormat PreferredFormat;

    /// <summary>Signaling attributes associated with the mapping.</summary>
    public string Attributes = "";

    internal RtpHeaderExtensionMapping Copy()
    {
        RtpHeaderExtensionMapping copy = new RtpHeaderExtensionMapping();
        copy.Identifier = Identifier;
        copy.Uri = Uri;
        copy.SupportsOneByte = SupportsOneByte;
        copy.SupportsTwoByte = SupportsTwoByte;
        copy.PreferredFormat = PreferredFormat;
        copy.Attributes = Attributes;
        return copy;
    }
}
