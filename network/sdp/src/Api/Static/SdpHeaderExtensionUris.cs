namespace sdp;

/// <summary>
/// Defines standardized and commonly deployed RTP header-extension URIs.
/// </summary>
public static class SdpHeaderExtensionUris
{
    /// <summary>The transmission-time-offset extension URI.</summary>
    public const string TransmissionTimeOffset = "urn:ietf:params:rtp-hdrext:toffset";

    /// <summary>The absolute send-time extension URI.</summary>
    public const string AbsoluteSendTime = "http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time";

    /// <summary>The absolute capture-time extension URI.</summary>
    public const string AbsoluteCaptureTime = "http://www.webrtc.org/experiments/rtp-hdrext/abs-capture-time";

    /// <summary>The synchronization-source audio-level extension URI.</summary>
    public const string SynchronizationSourceAudioLevel = "urn:ietf:params:rtp-hdrext:ssrc-audio-level";

    /// <summary>The contributing-source audio-level extension URI.</summary>
    public const string ContributingSourceAudioLevel = "urn:ietf:params:rtp-hdrext:csrc-audio-level";

    /// <summary>The encrypted header-extension marker URI.</summary>
    public const string Encryption = "urn:ietf:params:rtp-hdrext:encrypt";

    /// <summary>The coordination-of-video-orientation extension URI.</summary>
    public const string VideoOrientation = "urn:3gpp:video-orientation";

    /// <summary>The transport-wide congestion-control extension URI.</summary>
    public const string TransportWideCongestionControl = "http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01";

    /// <summary>The media-identification extension URI.</summary>
    public const string MediaId = "urn:ietf:params:rtp-hdrext:sdes:mid";

    /// <summary>The RTP stream-identifier extension URI.</summary>
    public const string RtpStreamId = "urn:ietf:params:rtp-hdrext:sdes:rtp-stream-id";

    /// <summary>The repaired RTP stream-identifier extension URI.</summary>
    public const string RepairedRtpStreamId = "urn:ietf:params:rtp-hdrext:sdes:repaired-rtp-stream-id";

    /// <summary>The playout-delay extension URI.</summary>
    public const string PlayoutDelay = "http://www.webrtc.org/experiments/rtp-hdrext/playout-delay";

    /// <summary>The video-content-type extension URI.</summary>
    public const string VideoContentType = "http://www.webrtc.org/experiments/rtp-hdrext/video-content-type";

    /// <summary>The video-timing extension URI.</summary>
    public const string VideoTiming = "http://www.webrtc.org/experiments/rtp-hdrext/video-timing";

    /// <summary>The video color-space extension URI.</summary>
    public const string ColorSpace = "http://www.webrtc.org/experiments/rtp-hdrext/color-space";

    /// <summary>The frame-marking extension URI.</summary>
    public const string FrameMarking = "urn:ietf:params:rtp-hdrext:framemarking";
}
