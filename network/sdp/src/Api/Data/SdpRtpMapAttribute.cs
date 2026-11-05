namespace sdp;

/// <summary>Represents a typed RTP payload mapping attribute.</summary>
public class SdpRtpMapAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the RTP payload type.</summary>
    public int PayloadType = 0;
    /// <summary>Contains the registered or extension encoding name.</summary>
    public string EncodingName = string.Empty;
    /// <summary>Contains the RTP clock rate in hertz.</summary>
    public int ClockRate = 0;
    /// <summary>Indicates whether encoding parameters were present.</summary>
    public bool HasEncodingParameters = false;
    /// <summary>Contains the original encoding-parameter token.</summary>
    public string EncodingParameters = string.Empty;
    /// <summary>Indicates whether a numeric channel count was recognized.</summary>
    public bool HasChannelCount = false;
    /// <summary>Contains the recognized channel count.</summary>
    public int ChannelCount = 1;
}
