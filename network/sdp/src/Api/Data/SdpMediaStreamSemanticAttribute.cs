using System.Collections.Generic;

namespace sdp;

/// <summary>Represents an MSID semantic and its associated media-stream identifiers.</summary>
public class SdpMediaStreamSemanticAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;

    /// <summary>Contains the registered or extension MSID semantic token.</summary>
    public string Semantic = string.Empty;

    /// <summary>Contains media-stream identifiers or the wildcard token in source order.</summary>
    public List<string> StreamIds = new List<string>();
}
