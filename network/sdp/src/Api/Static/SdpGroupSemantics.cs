namespace sdp;

/// <summary>
/// Defines registered semantics for the SDP group attribute.
/// </summary>
public static class SdpGroupSemantics
{
    /// <summary>The lip-synchronization semantic.</summary>
    public const string LipSynchronization = "LS";

    /// <summary>The flow-identification semantic.</summary>
    public const string FlowIdentification = "FID";

    /// <summary>The single-reservation-flow semantic.</summary>
    public const string SingleReservationFlow = "SRF";

    /// <summary>The alternative-network-address-types semantic.</summary>
    public const string AlternativeNetworkAddressTypes = "ANAT";

    /// <summary>The deprecated forward-error-correction semantic.</summary>
    public const string ForwardErrorCorrection = "FEC";

    /// <summary>The forward-error-correction framework semantic.</summary>
    public const string ForwardErrorCorrectionFramework = "FEC-FR";

    /// <summary>The composite-session semantic.</summary>
    public const string CompositeSession = "CS";

    /// <summary>The decoding-dependency semantic.</summary>
    public const string DecodingDependency = "DDP";

    /// <summary>The duplication semantic.</summary>
    public const string Duplication = "DUP";

    /// <summary>The synchronization-source simulcast semantic.</summary>
    public const string Simulcast = "SIM";

    /// <summary>The splice semantic.</summary>
    public const string Splice = "SPLICE";

    /// <summary>The media-bundling semantic.</summary>
    public const string Bundle = "BUNDLE";

    /// <summary>The CLUE-controlled media semantic.</summary>
    public const string Clue = "CLUE";

    /// <summary>The volumetric video grouping semantic.</summary>
    public const string V3c = "V3C";
}
