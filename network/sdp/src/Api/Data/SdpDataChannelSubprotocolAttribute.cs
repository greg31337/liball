namespace sdp;

/// <summary>Represents an SDP attribute embedded for a data-channel subprotocol.</summary>
public class SdpDataChannelSubprotocolAttribute
{
    /// <summary>Contains the originating generic <c>dcsa</c> attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;

    /// <summary>Contains the referenced SCTP stream identifier.</summary>
    public int ChannelId = 0;

    /// <summary>Contains the embedded subprotocol attribute.</summary>
    public SdpAttribute EmbeddedAttribute = new SdpAttribute();
}
