namespace sdp;

/// <summary>Represents an SCTP maximum-message-size attribute.</summary>
public class SdpMaximumMessageSizeAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the maximum message size in bytes, including zero for unlimited.</summary>
    public long MaximumMessageSize = 0;
}
