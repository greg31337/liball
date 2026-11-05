using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Contains registry metadata for an SDP attribute name.
/// </summary>
public class SdpAttributeRegistration
{
    /// <summary>Indicates whether the requested attribute has a registry record.</summary>
    public bool Found = false;

    /// <summary>Identifies the source registry.</summary>
    public string RegistryId = string.Empty;

    /// <summary>Contains the canonical registered attribute name.</summary>
    public string Name = string.Empty;

    /// <summary>Contains the unmodified registry usage-level text.</summary>
    public string ScopeText = string.Empty;

    /// <summary>Contains the typed scopes represented by the usage-level text.</summary>
    public List<SdpScope> Scopes = new List<SdpScope>();

    /// <summary>Contains the recognized multiplexing category.</summary>
    public SdpMuxCategory MuxCategory = SdpMuxCategory.Unknown;

    /// <summary>Contains the unmodified multiplexing-category token.</summary>
    public string MuxCategoryToken = string.Empty;

    /// <summary>Contains the normalized registration status.</summary>
    public string Status = "Unknown";

    /// <summary>Contains the registry references in source order.</summary>
    public List<string> References = new List<string>();
}
