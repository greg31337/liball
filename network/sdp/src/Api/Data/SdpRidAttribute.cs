using System.Collections.Generic;

namespace sdp;

/// <summary>Represents a typed RTP stream restriction attribute.</summary>
public class SdpRidAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the RID identifier.</summary>
    public string Id = string.Empty;
    /// <summary>Identifies the RID direction.</summary>
    public SdpDirection Direction = SdpDirection.Unknown;
    /// <summary>Contains payload types in source order.</summary>
    public List<int> PayloadTypes = new List<int>();
    /// <summary>Contains restriction parameters in source order, including duplicates.</summary>
    public List<SdpRidRestriction> Restrictions = new List<SdpRidRestriction>();
}
