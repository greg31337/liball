namespace sdp;

/// <summary>
/// Identifies why an offered media description was rejected during negotiation.
/// </summary>
public enum SdpRejectionReason
{
    /// <summary>The rejection reason is unknown or extension-defined.</summary>
    Unknown = 0,

    /// <summary>The offered media description was already rejected.</summary>
    OfferedAsRejected = 1,

    /// <summary>The offered media kind is unsupported.</summary>
    UnsupportedMedia = 2,

    /// <summary>The offered transport protocol is unsupported.</summary>
    UnsupportedProtocol = 3,

    /// <summary>No offered codec is compatible with local capabilities.</summary>
    NoCompatibleCodec = 4,

    /// <summary>The offered direction is incompatible with local capabilities.</summary>
    IncompatibleDirection = 5,

    /// <summary>The offer does not meet local ICE requirements.</summary>
    IceRequirementsNotMet = 6,

    /// <summary>The offer does not meet local TLS or DTLS requirements.</summary>
    DtlsRequirementsNotMet = 7,

    /// <summary>The offer does not meet a required RTP and RTCP multiplexing policy.</summary>
    RtcpMuxRequired = 8,

    /// <summary>The offer cannot satisfy local BUNDLE requirements.</summary>
    BundleRequirementsNotMet = 9,

    /// <summary>The offered data-channel configuration is incompatible.</summary>
    DataChannelRequirementsNotMet = 10,

    /// <summary>The media was rejected by local policy.</summary>
    RejectedByPolicy = 11,

    /// <summary>The media could not be negotiated because the offer is invalid.</summary>
    InvalidOffer = 12
}
