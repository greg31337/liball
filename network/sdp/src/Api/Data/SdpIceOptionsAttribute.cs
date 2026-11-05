using System.Collections.Generic;

namespace sdp;

/// <summary>Represents a typed SDP ICE options attribute.</summary>
public class SdpIceOptionsAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;

    /// <summary>Contains ICE option tokens in source order, including duplicates.</summary>
    public List<string> Options = new List<string>();
}
