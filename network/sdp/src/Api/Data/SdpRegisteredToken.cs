using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Contains registry metadata for a token or numeric identifier.
/// </summary>
public class SdpRegisteredToken
{
    /// <summary>Indicates whether the requested value has a registry record.</summary>
    public bool Found = false;

    /// <summary>Identifies the source registry.</summary>
    public string RegistryId = string.Empty;

    /// <summary>Contains the canonical token or registered numeric range.</summary>
    public string Token = string.Empty;

    /// <summary>Contains the registered name.</summary>
    public string Name = string.Empty;

    /// <summary>Contains the registered description when one is available.</summary>
    public string Description = string.Empty;

    /// <summary>Contains the recognized multiplexing category when one is registered.</summary>
    public SdpMuxCategory MuxCategory = SdpMuxCategory.Unknown;

    /// <summary>Contains the unmodified multiplexing-category token.</summary>
    public string MuxCategoryToken = string.Empty;

    /// <summary>Contains the normalized registration status.</summary>
    public string Status = "Unknown";

    /// <summary>Contains the registry references in source order.</summary>
    public List<string> References = new List<string>();
}
