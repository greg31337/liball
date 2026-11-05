namespace sdp;

/// <summary>Represents a retained legacy SCTP port-to-application mapping.</summary>
public class SdpSctpMapAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the SCTP port from zero through 65535.</summary>
    public int Port = 0;
    /// <summary>Contains the SCTP application-format token.</summary>
    public string Format = string.Empty;
    /// <summary>Indicates whether an explicit incoming-stream count was present.</summary>
    public bool HasMaximumStreams = false;
    /// <summary>Contains the explicit incoming-stream count when present.</summary>
    public int MaximumStreams = 0;
}
