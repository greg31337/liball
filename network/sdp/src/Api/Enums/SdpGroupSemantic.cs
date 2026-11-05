namespace sdp;

/// <summary>
/// Identifies a standardized semantic for grouping SDP media descriptions.
/// </summary>
public enum SdpGroupSemantic
{
    /// <summary>The group semantic is unknown or extension-defined.</summary>
    Unknown = 0,

    /// <summary>The grouped media are lip-synchronized.</summary>
    LipSynchronization = 1,

    /// <summary>The grouped media identify components of a single flow.</summary>
    FlowIdentification = 2,

    /// <summary>The grouped media share a single reservation flow.</summary>
    SingleReservationFlow = 3,

    /// <summary>The grouped media are alternatives using different network address types.</summary>
    AlternativeNetworkAddressTypes = 4,

    /// <summary>The grouped media have a forward-error-correction relationship.</summary>
    ForwardErrorCorrection = 5,

    /// <summary>The grouped media have a decoding dependency.</summary>
    DecodingDependency = 6,

    /// <summary>The grouped media use a shared BUNDLE transport.</summary>
    Bundle = 7,

    /// <summary>The grouped media use the forward-error-correction framework.</summary>
    ForwardErrorCorrectionFramework = 8,

    /// <summary>The grouped media form a composite session.</summary>
    CompositeSession = 9,

    /// <summary>The grouped media carry duplicate content.</summary>
    Duplication = 10,

    /// <summary>The grouped media participate in a splice operation.</summary>
    Splice = 11,

    /// <summary>The grouped media are controlled by a CLUE telepresence description.</summary>
    Clue = 12,

    /// <summary>The grouped media carry volumetric video components.</summary>
    VolumetricVideo = 13
}
