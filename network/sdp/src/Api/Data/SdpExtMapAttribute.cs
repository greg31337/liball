namespace sdp;

/// <summary>Represents a typed RTP header-extension mapping attribute.</summary>
public class SdpExtMapAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the extension identifier.</summary>
    public int Id = 0;
    /// <summary>Indicates whether an explicit direction qualifier was present.</summary>
    public bool HasDirection = false;
    /// <summary>Identifies the extension direction.</summary>
    public SdpDirection Direction = SdpDirection.SendReceive;
    /// <summary>Indicates whether the encrypted extension form was requested.</summary>
    public bool Encrypted = false;
    /// <summary>Contains the extension URI.</summary>
    public string Uri = string.Empty;
    /// <summary>Contains optional extension-specific attributes.</summary>
    public string ExtensionAttributes = string.Empty;
}
