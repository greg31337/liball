namespace sdp;

/// <summary>
/// Defines RTP encoding names used by static mappings and common negotiated codecs.
/// </summary>
public static class SdpCodecNames
{
    /// <summary>The G.711 mu-law encoding name.</summary>
    public const string Pcmu = "PCMU";

    /// <summary>The GSM encoding name.</summary>
    public const string Gsm = "GSM";

    /// <summary>The G.723 encoding name.</summary>
    public const string G723 = "G723";

    /// <summary>The DVI4 encoding name.</summary>
    public const string Dvi4 = "DVI4";

    /// <summary>The linear predictive coding encoding name.</summary>
    public const string Lpc = "LPC";

    /// <summary>The G.711 A-law encoding name.</summary>
    public const string Pcma = "PCMA";

    /// <summary>The G.722 encoding name.</summary>
    public const string G722 = "G722";

    /// <summary>The linear 16-bit audio encoding name.</summary>
    public const string Linear16 = "L16";

    /// <summary>The QCELP encoding name.</summary>
    public const string Qcelp = "QCELP";

    /// <summary>The comfort-noise encoding name.</summary>
    public const string ComfortNoise = "CN";

    /// <summary>The MPEG audio encoding name.</summary>
    public const string MpegAudio = "MPA";

    /// <summary>The G.728 encoding name.</summary>
    public const string G728 = "G728";

    /// <summary>The G.729 encoding name.</summary>
    public const string G729 = "G729";

    /// <summary>The Cell-B video encoding name.</summary>
    public const string CellB = "CelB";

    /// <summary>The JPEG video encoding name.</summary>
    public const string Jpeg = "JPEG";

    /// <summary>The Xerox PARC video encoding name.</summary>
    public const string Nv = "nv";

    /// <summary>The H.261 video encoding name.</summary>
    public const string H261 = "H261";

    /// <summary>The MPEG video encoding name.</summary>
    public const string MpegVideo = "MPV";

    /// <summary>The MPEG-2 transport-stream encoding name.</summary>
    public const string Mpeg2TransportStream = "MP2T";

    /// <summary>The H.263 video encoding name.</summary>
    public const string H263 = "H263";

    /// <summary>The Opus audio encoding name.</summary>
    public const string Opus = "opus";

    /// <summary>The VP8 video encoding name.</summary>
    public const string Vp8 = "VP8";

    /// <summary>The VP9 video encoding name.</summary>
    public const string Vp9 = "VP9";

    /// <summary>The AV1 video encoding name.</summary>
    public const string Av1 = "AV1";

    /// <summary>The H.264 video encoding name.</summary>
    public const string H264 = "H264";

    /// <summary>The H.265 video encoding name.</summary>
    public const string H265 = "H265";

    /// <summary>The HEVC video encoding name registered as an alias for H.265 media.</summary>
    public const string Hevc = "HEVC";

    /// <summary>The retransmission encoding name.</summary>
    public const string Retransmission = "rtx";

    /// <summary>The redundant-audio-data encoding name.</summary>
    public const string RedundantAudioData = "red";

    /// <summary>The unequal-loss-protection FEC encoding name.</summary>
    public const string UnequalLossProtectionFec = "ulpfec";

    /// <summary>The flexible FEC encoding name.</summary>
    public const string FlexibleFec = "flexfec";

    /// <summary>The telephone-event encoding name.</summary>
    public const string TelephoneEvent = "telephone-event";
}
