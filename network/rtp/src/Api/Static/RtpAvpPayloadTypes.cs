namespace rtp;

/// <summary>
/// Defines RFC 3551 static payload assignments and their RTP clock rates.
/// </summary>
public static class RtpAvpPayloadTypes
{
    /// <summary>The static payload type for PCMU audio.</summary>
    public const byte Pcmu = 0;

    /// <summary>The RTP clock rate for PCMU audio.</summary>
    public const int PcmuClockRate = 8000;

    /// <summary>The static payload type for GSM audio.</summary>
    public const byte Gsm = 3;

    /// <summary>The RTP clock rate for GSM audio.</summary>
    public const int GsmClockRate = 8000;

    /// <summary>The static payload type for G.723 audio.</summary>
    public const byte G723 = 4;

    /// <summary>The RTP clock rate for G.723 audio.</summary>
    public const int G723ClockRate = 8000;

    /// <summary>The static payload type for 8 kHz DVI4 audio.</summary>
    public const byte Dvi4At8000 = 5;

    /// <summary>The RTP clock rate for the 8 kHz DVI4 assignment.</summary>
    public const int Dvi4At8000ClockRate = 8000;

    /// <summary>The static payload type for 16 kHz DVI4 audio.</summary>
    public const byte Dvi4At16000 = 6;

    /// <summary>The RTP clock rate for the 16 kHz DVI4 assignment.</summary>
    public const int Dvi4At16000ClockRate = 16000;

    /// <summary>The static payload type for LPC audio.</summary>
    public const byte Lpc = 7;

    /// <summary>The RTP clock rate for LPC audio.</summary>
    public const int LpcClockRate = 8000;

    /// <summary>The static payload type for PCMA audio.</summary>
    public const byte Pcma = 8;

    /// <summary>The RTP clock rate for PCMA audio.</summary>
    public const int PcmaClockRate = 8000;

    /// <summary>The static payload type for G.722 audio.</summary>
    public const byte G722 = 9;

    /// <summary>The RTP clock rate for G.722 audio.</summary>
    public const int G722ClockRate = 8000;

    /// <summary>The static payload type for stereo L16 audio.</summary>
    public const byte L16Stereo = 10;

    /// <summary>The RTP clock rate for stereo L16 audio.</summary>
    public const int L16StereoClockRate = 44100;

    /// <summary>The static payload type for mono L16 audio.</summary>
    public const byte L16Mono = 11;

    /// <summary>The RTP clock rate for mono L16 audio.</summary>
    public const int L16MonoClockRate = 44100;

    /// <summary>The static payload type for QCELP audio.</summary>
    public const byte Qcelp = 12;

    /// <summary>The RTP clock rate for QCELP audio.</summary>
    public const int QcelpClockRate = 8000;

    /// <summary>The static payload type for comfort noise.</summary>
    public const byte ComfortNoise = 13;

    /// <summary>The RTP clock rate for the static comfort-noise assignment.</summary>
    public const int ComfortNoiseClockRate = 8000;

    /// <summary>The static payload type for MPEG audio.</summary>
    public const byte MpegAudio = 14;

    /// <summary>The RTP clock rate for MPEG audio.</summary>
    public const int MpegAudioClockRate = 90000;

    /// <summary>The static payload type for G.728 audio.</summary>
    public const byte G728 = 15;

    /// <summary>The RTP clock rate for G.728 audio.</summary>
    public const int G728ClockRate = 8000;

    /// <summary>The static payload type for 11.025 kHz DVI4 audio.</summary>
    public const byte Dvi4At11025 = 16;

    /// <summary>The RTP clock rate for the 11.025 kHz DVI4 assignment.</summary>
    public const int Dvi4At11025ClockRate = 11025;

    /// <summary>The static payload type for 22.05 kHz DVI4 audio.</summary>
    public const byte Dvi4At22050 = 17;

    /// <summary>The RTP clock rate for the 22.05 kHz DVI4 assignment.</summary>
    public const int Dvi4At22050ClockRate = 22050;

    /// <summary>The static payload type for G.729 audio.</summary>
    public const byte G729 = 18;

    /// <summary>The RTP clock rate for G.729 audio.</summary>
    public const int G729ClockRate = 8000;

    /// <summary>The static payload type for Cell-B video.</summary>
    public const byte CelB = 25;

    /// <summary>The RTP clock rate for Cell-B video.</summary>
    public const int CelBClockRate = 90000;

    /// <summary>The static payload type for JPEG video.</summary>
    public const byte Jpeg = 26;

    /// <summary>The RTP clock rate for JPEG video.</summary>
    public const int JpegClockRate = 90000;

    /// <summary>The static payload type for nv video.</summary>
    public const byte Nv = 28;

    /// <summary>The RTP clock rate for nv video.</summary>
    public const int NvClockRate = 90000;

    /// <summary>The static payload type for H.261 video.</summary>
    public const byte H261 = 31;

    /// <summary>The RTP clock rate for H.261 video.</summary>
    public const int H261ClockRate = 90000;

    /// <summary>The static payload type for MPEG video.</summary>
    public const byte MpegVideo = 32;

    /// <summary>The RTP clock rate for MPEG video.</summary>
    public const int MpegVideoClockRate = 90000;

    /// <summary>The static payload type for MPEG-2 transport streams.</summary>
    public const byte Mpeg2TransportStream = 33;

    /// <summary>The RTP clock rate for MPEG-2 transport streams.</summary>
    public const int Mpeg2TransportStreamClockRate = 90000;

    /// <summary>The static payload type for H.263 video.</summary>
    public const byte H263 = 34;

    /// <summary>The RTP clock rate for H.263 video.</summary>
    public const int H263ClockRate = 90000;

    /// <summary>The first payload type reserved for RTCP conflict avoidance.</summary>
    public const byte RtcpConflictAvoidanceMinimum = 72;

    /// <summary>The last payload type reserved for RTCP conflict avoidance.</summary>
    public const byte RtcpConflictAvoidanceMaximum = 76;

    /// <summary>The first payload type preferred for dynamic assignment.</summary>
    public const byte DynamicMinimum = 96;

    /// <summary>The last payload type available for dynamic assignment.</summary>
    public const byte DynamicMaximum = 127;

    /// <summary>The channel count used by mono static audio assignments.</summary>
    public const int MonoChannelCount = 1;

    /// <summary>The channel count used by the stereo L16 static assignment.</summary>
    public const int StereoChannelCount = 2;
}
