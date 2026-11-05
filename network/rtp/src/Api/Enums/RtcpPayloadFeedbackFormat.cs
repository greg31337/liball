namespace rtp;

/// <summary>
/// Identifies a registered PSFB feedback-message format.
/// </summary>
public enum RtcpPayloadFeedbackFormat
{
    /// <summary>The format is unsupported or not recognized.</summary>
    Unknown = 0,

    /// <summary>The message is a picture loss indication.</summary>
    PictureLossIndication = 1,

    /// <summary>The message is a slice loss indication.</summary>
    SliceLossIndication = 2,

    /// <summary>The message is a reference picture selection indication.</summary>
    ReferencePictureSelectionIndication = 3,

    /// <summary>The message is a full intra request.</summary>
    FullIntraRequest = 4,

    /// <summary>The message is a temporal-spatial trade-off request.</summary>
    TemporalSpatialTradeoffRequest = 5,

    /// <summary>The message is a temporal-spatial trade-off notification.</summary>
    TemporalSpatialTradeoffNotification = 6,

    /// <summary>The message contains video back-channel data.</summary>
    VideoBackChannelMessage = 7,

    /// <summary>The message contains payload-specific third-party loss indication.</summary>
    ThirdPartyLossEarlyIndication = 8,

    /// <summary>The message identifies a video region of interest.</summary>
    RegionOfInterest = 9,

    /// <summary>The message is a layer refresh request.</summary>
    LayerRefreshRequest = 10,

    /// <summary>The message contains viewport feedback.</summary>
    Viewport = 11,

    /// <summary>The message contains application-layer feedback.</summary>
    ApplicationFeedback = 15,

    /// <summary>The format is reserved for future extensions.</summary>
    Extension = 31
}
