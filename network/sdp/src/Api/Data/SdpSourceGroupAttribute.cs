using System.Collections.Generic;

namespace sdp;

/// <summary>Represents an ordered group of RTP synchronization sources.</summary>
public class SdpSourceGroupAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the registered or extension grouping semantic.</summary>
    public string Semantic = string.Empty;
    /// <summary>Contains synchronization-source identifiers in source order.</summary>
    public List<uint> SynchronizationSources = new List<uint>();
}
