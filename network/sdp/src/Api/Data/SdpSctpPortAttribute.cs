namespace sdp;

/// <summary>Represents an SCTP port attribute.</summary>
public class SdpSctpPortAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the SCTP port from zero through 65535.</summary>
    public int Port = 0;
}
