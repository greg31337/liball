namespace sdp;

/// <summary>Maps an offered header-extension identifier to a negotiated identifier.</summary>
public class SdpHeaderExtensionMapping
{
    /// <summary>Contains the offered extension identifier.</summary>
    public int OfferedId = 0;
    /// <summary>Contains the negotiated extension identifier.</summary>
    public int NegotiatedId = 0;
    /// <summary>Contains the extension URI.</summary>
    public string Uri = string.Empty;
    /// <summary>Contains the negotiated direction.</summary>
    public SdpDirection Direction = SdpDirection.Unknown;
    /// <summary>Indicates whether the negotiated mapping is encrypted.</summary>
    public bool Encrypted = false;
    /// <summary>Contains the extension-specific attributes identifying the negotiated configuration.</summary>
    public string ExtensionAttributes = string.Empty;
}
