namespace sdp;

/// <summary>Represents a typed SDP ICE password attribute.</summary>
public class SdpIcePasswordAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;

    /// <summary>Contains the ICE password and must be treated as sensitive data.</summary>
    public string Password = string.Empty;
}
