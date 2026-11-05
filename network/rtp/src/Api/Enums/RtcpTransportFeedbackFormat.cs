namespace rtp;

/// <summary>
/// Identifies a registered RTPFB feedback-message format.
/// </summary>
public enum RtcpTransportFeedbackFormat
{
    /// <summary>The format is unsupported or not recognized.</summary>
    Unknown = 0,

    /// <summary>The message contains Generic NACK feedback.</summary>
    GenericNack = 1,

    /// <summary>The format value is reserved by RFC 5104.</summary>
    Reserved = 2,

    /// <summary>The message requests a temporary maximum media bitrate.</summary>
    TemporaryMaximumBitrateRequest = 3,

    /// <summary>The message announces a temporary maximum media bitrate.</summary>
    TemporaryMaximumBitrateNotification = 4,

    /// <summary>The message requests rapid RTCP synchronization.</summary>
    RapidResynchronizationRequest = 5,

    /// <summary>The message requests rapid multicast acquisition.</summary>
    RapidMulticastAcquisition = 6,

    /// <summary>The message contains transport-layer third-party loss indication.</summary>
    ThirdPartyLossEarlyIndication = 7,

    /// <summary>The message contains ECN feedback.</summary>
    EcnFeedback = 8,

    /// <summary>The message requests media pause or resume.</summary>
    PauseResume = 9,

    /// <summary>The message contains delay-budget information.</summary>
    DelayBudgetInformation = 10,

    /// <summary>The message contains RFC 8888 congestion-control feedback.</summary>
    CongestionControlFeedback = 11,

    /// <summary>The format is reserved for future extensions.</summary>
    Extension = 31
}
