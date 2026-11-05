using System.Collections.Generic;

namespace sdp;

/// <summary>Describes one locally supported codec configuration.</summary>
public class SdpCodecCapability
{
    /// <summary>Identifies the recognized media kind.</summary>
    public SdpMediaKind MediaKind = SdpMediaKind.Unknown;
    /// <summary>Contains the registered or extension media-type token.</summary>
    public string MediaType = string.Empty;
    /// <summary>Contains the registered or extension encoding name.</summary>
    public string EncodingName = string.Empty;
    /// <summary>Contains the RTP clock rate in hertz.</summary>
    public int ClockRate = 0;
    /// <summary>Indicates whether a preferred RTP payload type was supplied.</summary>
    public bool HasPreferredPayloadType = false;
    /// <summary>Contains the preferred RTP payload type when supplied.</summary>
    public int PreferredPayloadType = 0;
    /// <summary>Indicates whether a channel count constrains matching.</summary>
    public bool HasChannelCount = false;
    /// <summary>Contains the supported channel count.</summary>
    public int ChannelCount = 1;
    /// <summary>Contains supported format parameters in preference order.</summary>
    public List<SdpFormatParameter> FormatParameters = new List<SdpFormatParameter>();
    /// <summary>Contains supported RTCP feedback type tokens.</summary>
    public List<string> RtcpFeedbackTypes = new List<string>();
    /// <summary>Contains detailed supported RTCP feedback configurations.</summary>
    public List<SdpRtcpFeedbackAttribute> RtcpFeedback = new List<SdpRtcpFeedbackAttribute>();
    /// <summary>Contains the lower-is-more-preferred ordering value.</summary>
    public int Preference = 0;
}
