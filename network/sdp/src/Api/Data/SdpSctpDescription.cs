namespace sdp;

/// <summary>Collects SCTP media and data-channel transport signaling.</summary>
public class SdpSctpDescription
{
    /// <summary>Contains the selected modern SCTP port attribute when present.</summary>
    public SdpSctpPortAttribute? PortAttribute = null;
    /// <summary>Contains the selected maximum-message-size attribute when present.</summary>
    public SdpMaximumMessageSizeAttribute? MaximumMessageSizeAttribute = null;
    /// <summary>Contains the selected legacy SCTP mapping when present.</summary>
    public SdpSctpMapAttribute? SctpMapAttribute = null;
    /// <summary>Contains the SCTP port.</summary>
    public int Port = 0;
    /// <summary>Indicates whether an explicit maximum message size was present.</summary>
    public bool HasMaximumMessageSize = false;
    /// <summary>Contains the maximum message size in bytes, including zero for unlimited.</summary>
    public long MaximumMessageSize = 0;
    /// <summary>Contains the SCTP application-format token.</summary>
    public string Format = string.Empty;
    /// <summary>Indicates that legacy <c>sctpmap</c> syntax supplied the values.</summary>
    public bool LegacySctpMap = false;
    /// <summary>Indicates whether legacy signaling supplied an incoming-stream count.</summary>
    public bool HasMaximumStreams = false;
    /// <summary>Contains the legacy incoming-stream count when present.</summary>
    public int MaximumStreams = 0;
}
