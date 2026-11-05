using System.Collections.Generic;

namespace sdp;

/// <summary>Represents a typed SDP media-group attribute.</summary>
public class SdpGroupAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Identifies a recognized grouping semantic.</summary>
    public SdpGroupSemantic Semantic = SdpGroupSemantic.Unknown;
    /// <summary>Contains the original grouping-semantic token.</summary>
    public string SemanticToken = string.Empty;
    /// <summary>Contains identification tags in source order.</summary>
    public List<string> IdentificationTags = new List<string>();
}
