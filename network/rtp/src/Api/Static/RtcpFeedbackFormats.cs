namespace rtp;

/// <summary>
/// Defines registered transport-layer and payload-specific RTCP feedback formats.
/// </summary>
public static class RtcpFeedbackFormats
{
    /// <summary>The RTPFB format for Generic NACK.</summary>
    public const byte TransportGenericNack = 1;

    /// <summary>The RTPFB format reserved by RFC 5104.</summary>
    public const byte TransportReserved = 2;

    /// <summary>The RTPFB format for temporary maximum media stream bitrate requests.</summary>
    public const byte TransportTemporaryMaximumBitrateRequest = 3;

    /// <summary>The RTPFB format for temporary maximum media stream bitrate notifications.</summary>
    public const byte TransportTemporaryMaximumBitrateNotification = 4;

    /// <summary>The RTPFB format for rapid resynchronization requests.</summary>
    public const byte TransportRapidResynchronizationRequest = 5;

    /// <summary>The RTPFB format for rapid acquisition of multicast sessions.</summary>
    public const byte TransportRapidMulticastAcquisition = 6;

    /// <summary>The RTPFB format for transport-layer third-party loss early indication.</summary>
    public const byte TransportThirdPartyLossEarlyIndication = 7;

    /// <summary>The RTPFB format for ECN feedback.</summary>
    public const byte TransportEcnFeedback = 8;

    /// <summary>The RTPFB format for media pause and resume.</summary>
    public const byte TransportPauseResume = 9;

    /// <summary>The RTPFB format for delay budget information.</summary>
    public const byte TransportDelayBudgetInformation = 10;

    /// <summary>The RTPFB format for RFC 8888 congestion-control feedback.</summary>
    public const byte TransportCongestionControlFeedback = 11;

    /// <summary>The RTPFB format reserved for future extensions.</summary>
    public const byte TransportExtension = 31;

    /// <summary>The PSFB format for picture loss indication.</summary>
    public const byte PayloadPictureLossIndication = 1;

    /// <summary>The PSFB format for slice loss indication.</summary>
    public const byte PayloadSliceLossIndication = 2;

    /// <summary>The PSFB format for reference picture selection indication.</summary>
    public const byte PayloadReferencePictureSelectionIndication = 3;

    /// <summary>The PSFB format for full intra requests.</summary>
    public const byte PayloadFullIntraRequest = 4;

    /// <summary>The PSFB format for temporal-spatial trade-off requests.</summary>
    public const byte PayloadTemporalSpatialTradeoffRequest = 5;

    /// <summary>The PSFB format for temporal-spatial trade-off notifications.</summary>
    public const byte PayloadTemporalSpatialTradeoffNotification = 6;

    /// <summary>The PSFB format for video back-channel messages.</summary>
    public const byte PayloadVideoBackChannelMessage = 7;

    /// <summary>The PSFB format for payload-specific third-party loss early indication.</summary>
    public const byte PayloadThirdPartyLossEarlyIndication = 8;

    /// <summary>The PSFB format for video regions of interest.</summary>
    public const byte PayloadRegionOfInterest = 9;

    /// <summary>The PSFB format for layer refresh requests.</summary>
    public const byte PayloadLayerRefreshRequest = 10;

    /// <summary>The PSFB format for viewport feedback.</summary>
    public const byte PayloadViewport = 11;

    /// <summary>The PSFB format for application-layer feedback.</summary>
    public const byte PayloadApplicationFeedback = 15;

    /// <summary>The PSFB format reserved for future extensions.</summary>
    public const byte PayloadExtension = 31;
}
