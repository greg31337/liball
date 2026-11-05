using System.Collections.Generic;

namespace sdp;

/// <summary>Represents a typed data-channel mapping attribute.</summary>
public class SdpDataChannelAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the SCTP stream identifier.</summary>
    public int ChannelId = 0;
    /// <summary>Contains mapping parameters in source order, including duplicates.</summary>
    public List<SdpDataChannelParameter> Parameters = new List<SdpDataChannelParameter>();
    /// <summary>Contains associated <c>dcsa</c> attributes in source order.</summary>
    public List<SdpAttribute> SubprotocolAttributes = new List<SdpAttribute>();
}
