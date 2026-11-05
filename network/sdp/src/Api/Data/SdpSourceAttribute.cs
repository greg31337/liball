namespace sdp;

/// <summary>Represents one typed synchronization-source attribute.</summary>
public class SdpSourceAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the unsigned synchronization-source identifier.</summary>
    public uint SynchronizationSource = 0;
    /// <summary>Contains the source attribute name.</summary>
    public string Name = string.Empty;
    /// <summary>Indicates whether the source attribute has a value.</summary>
    public bool HasValue = false;
    /// <summary>Contains the source attribute value.</summary>
    public string Value = string.Empty;
}
