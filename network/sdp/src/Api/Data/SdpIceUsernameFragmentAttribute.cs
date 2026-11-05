namespace sdp;

/// <summary>Represents a typed SDP ICE username-fragment attribute.</summary>
public class SdpIceUsernameFragmentAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;

    /// <summary>Contains the ICE username fragment.</summary>
    public string UsernameFragment = string.Empty;
}
