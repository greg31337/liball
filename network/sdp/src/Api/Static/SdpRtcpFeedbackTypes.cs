namespace sdp;

/// <summary>
/// Defines common RTCP feedback type and parameter tokens.
/// </summary>
public static class SdpRtcpFeedbackTypes
{
    /// <summary>The positive-acknowledgement feedback type.</summary>
    public const string Acknowledgement = "ack";

    /// <summary>The negative-acknowledgement feedback type.</summary>
    public const string NegativeAcknowledgement = "nack";

    /// <summary>The minimum regular RTCP interval feedback type.</summary>
    public const string MinimumInterval = "trr-int";

    /// <summary>The codec-control-message feedback type.</summary>
    public const string CodecControlMessage = "ccm";

    /// <summary>The application-layer feedback parameter.</summary>
    public const string Application = "app";

    /// <summary>The picture-loss-indication feedback parameter.</summary>
    public const string PictureLossIndication = "pli";

    /// <summary>The slice-loss-indication feedback parameter.</summary>
    public const string SliceLossIndication = "sli";

    /// <summary>The reference-picture-selection-indication feedback parameter.</summary>
    public const string ReferencePictureSelectionIndication = "rpsi";

    /// <summary>The full-intra-request feedback parameter.</summary>
    public const string FullIntraRequest = "fir";

    /// <summary>The temporary-maximum-media-bit-rate feedback parameter.</summary>
    public const string TemporaryMaximumMediaBitRate = "tmmbr";

    /// <summary>The temporary-maximum-media-bit-rate-notification feedback parameter.</summary>
    public const string TemporaryMaximumMediaBitRateNotification = "tmmbn";

    /// <summary>The temporal-spatial-trade-off-request feedback parameter.</summary>
    public const string TemporalSpatialTradeOffRequest = "tstr";

    /// <summary>The temporal-spatial-trade-off-notification feedback parameter.</summary>
    public const string TemporalSpatialTradeOffNotification = "tstn";

    /// <summary>The video-back-channel-message feedback parameter.</summary>
    public const string VideoBackChannelMessage = "vbcm";

    /// <summary>The transport-wide congestion-control feedback type.</summary>
    public const string TransportWideCongestionControl = "transport-cc";

    /// <summary>The commonly used receiver-estimated maximum bitrate feedback type.</summary>
    public const string ReceiverEstimatedMaximumBitrate = "goog-remb";
}
