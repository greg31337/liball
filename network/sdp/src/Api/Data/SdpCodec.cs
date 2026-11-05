using System.Collections.Generic;

namespace sdp;

/// <summary>Represents an RTP codec projected from one media description.</summary>
public class SdpCodec
{
    /// <summary>Contains the RTP payload type.</summary>
    public int PayloadType = 0;
    /// <summary>Identifies the recognized media kind.</summary>
    public SdpMediaKind MediaKind = SdpMediaKind.Unknown;
    /// <summary>Contains the registered or extension media-type token.</summary>
    public string MediaType = string.Empty;
    /// <summary>Contains the registered or extension encoding name.</summary>
    public string EncodingName = string.Empty;
    /// <summary>Contains the RTP clock rate in hertz.</summary>
    public int ClockRate = 0;
    /// <summary>Indicates whether an explicit channel count was present.</summary>
    public bool HasChannelCount = false;
    /// <summary>Contains the channel count.</summary>
    public int ChannelCount = 1;
    /// <summary>Contains the source RTP mapping when available.</summary>
    public SdpRtpMapAttribute? RtpMap = null;
    /// <summary>Contains the media-level packet time when available.</summary>
    public SdpCommonAttribute? PacketTime = null;
    /// <summary>Contains the media-level maximum packet time when available.</summary>
    public SdpCommonAttribute? MaximumPacketTime = null;
    /// <summary>Contains format-parameter attributes in source order.</summary>
    public List<SdpFormatParametersAttribute> FormatParameters = new List<SdpFormatParametersAttribute>();
    /// <summary>Contains RTCP feedback attributes in source order.</summary>
    public List<SdpRtcpFeedbackAttribute> RtcpFeedback = new List<SdpRtcpFeedbackAttribute>();
}
