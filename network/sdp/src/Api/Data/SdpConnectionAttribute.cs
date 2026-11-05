namespace sdp;

/// <summary>Represents a connection-oriented media connection-reuse attribute.</summary>
public class SdpConnectionAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;

    /// <summary>Contains the registered or extension connection-reuse token.</summary>
    public string ConnectionRole = string.Empty;
}
