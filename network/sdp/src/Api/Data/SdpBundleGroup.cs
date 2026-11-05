using System.Collections.Generic;

namespace sdp;

/// <summary>Represents a resolved BUNDLE group.</summary>
public class SdpBundleGroup
{
    /// <summary>Contains the source group attribute when available.</summary>
    public SdpGroupAttribute? SourceGroup = null;
    /// <summary>Contains bundled media identifiers in source order.</summary>
    public List<string> MediaIds = new List<string>();
    /// <summary>Contains the tagged media identifier supplying the bundle transport.</summary>
    public string TaggedMediaId = string.Empty;
}
