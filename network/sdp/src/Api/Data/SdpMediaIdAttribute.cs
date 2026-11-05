namespace sdp;

/// <summary>Represents an SDP media identification attribute.</summary>
public class SdpMediaIdAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;

    /// <summary>Contains the media identification tag.</summary>
    public string Id = string.Empty;
}
