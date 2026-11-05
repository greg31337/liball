namespace sdp;

/// <summary>
/// Defines RTP audio/video profile payload-type assignments and dynamic bounds.
/// </summary>
public static class SdpStaticPayloadTypes
{
    /// <summary>The PCMU payload type.</summary>
    public const int Pcmu = 0;

    /// <summary>The GSM payload type.</summary>
    public const int Gsm = 3;

    /// <summary>The G.723 payload type.</summary>
    public const int G723 = 4;

    /// <summary>The 8 kHz DVI4 payload type.</summary>
    public const int Dvi4_8000 = 5;

    /// <summary>The 16 kHz DVI4 payload type.</summary>
    public const int Dvi4_16000 = 6;

    /// <summary>The LPC payload type.</summary>
    public const int Lpc = 7;

    /// <summary>The PCMA payload type.</summary>
    public const int Pcma = 8;

    /// <summary>The G.722 payload type.</summary>
    public const int G722 = 9;

    /// <summary>The stereo linear 16-bit audio payload type.</summary>
    public const int Linear16Stereo = 10;

    /// <summary>The mono linear 16-bit audio payload type.</summary>
    public const int Linear16Mono = 11;

    /// <summary>The QCELP payload type.</summary>
    public const int Qcelp = 12;

    /// <summary>The 8 kHz comfort-noise payload type.</summary>
    public const int ComfortNoise = 13;

    /// <summary>The MPEG audio payload type.</summary>
    public const int MpegAudio = 14;

    /// <summary>The G.728 payload type.</summary>
    public const int G728 = 15;

    /// <summary>The 11.025 kHz DVI4 payload type.</summary>
    public const int Dvi4_11025 = 16;

    /// <summary>The 22.05 kHz DVI4 payload type.</summary>
    public const int Dvi4_22050 = 17;

    /// <summary>The G.729 payload type.</summary>
    public const int G729 = 18;

    /// <summary>The Cell-B video payload type.</summary>
    public const int CellB = 25;

    /// <summary>The JPEG video payload type.</summary>
    public const int Jpeg = 26;

    /// <summary>The Xerox PARC video payload type.</summary>
    public const int Nv = 28;

    /// <summary>The H.261 video payload type.</summary>
    public const int H261 = 31;

    /// <summary>The MPEG video payload type.</summary>
    public const int MpegVideo = 32;

    /// <summary>The MPEG-2 transport-stream payload type.</summary>
    public const int Mpeg2TransportStream = 33;

    /// <summary>The H.263 video payload type.</summary>
    public const int H263 = 34;

    /// <summary>The first dynamically assigned payload type.</summary>
    public const int DynamicMinimum = 96;

    /// <summary>The last dynamically assigned payload type.</summary>
    public const int DynamicMaximum = 127;
}
