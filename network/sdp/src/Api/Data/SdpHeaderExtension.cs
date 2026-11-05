namespace sdp;

/// <summary>Describes one supported RTP header extension.</summary>
public class SdpHeaderExtension
{
    /// <summary>Contains the extension URI.</summary>
    public string Uri = string.Empty;
    /// <summary>Indicates whether a preferred local identifier was supplied.</summary>
    public bool HasPreferredId = false;
    /// <summary>Contains the preferred local identifier when supplied.</summary>
    public int PreferredId = 0;
    /// <summary>Contains the supported direction.</summary>
    public SdpDirection Direction = SdpDirection.SendReceive;
    /// <summary>Indicates support for the encrypted extension form.</summary>
    public bool SupportsEncryption = false;
    /// <summary>Contains extension-specific attributes that constrain the supported configuration.</summary>
    public string ExtensionAttributes = string.Empty;
    /// <summary>Indicates that negotiation must retain this extension when its media is accepted.</summary>
    public bool Required = false;
    /// <summary>Contains the lower-is-more-preferred ordering value.</summary>
    public int Preference = 0;
}
