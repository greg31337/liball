namespace sdp;

/// <summary>
/// Identifies the rule set used to validate an SDP description.
/// </summary>
public enum SdpValidationProfile
{
    /// <summary>The validation profile is unknown or was not specified.</summary>
    Unknown = 0,

    /// <summary>Apply syntax rules without protocol-profile semantics.</summary>
    SyntaxOnly = 1,

    /// <summary>Apply the core semantic rules from RFC 8866.</summary>
    Rfc8866 = 2,

    /// <summary>Apply Interactive Connectivity Establishment rules.</summary>
    Ice = 3,

    /// <summary>Apply TLS and Datagram TLS signaling rules.</summary>
    TlsDtls = 4,

    /// <summary>Apply Real-time Transport Protocol signaling rules.</summary>
    Rtp = 5,

    /// <summary>Apply WebRTC SDP rules.</summary>
    WebRtc = 6,

    /// <summary>Apply BUNDLE and grouping rules.</summary>
    Bundle = 7,

    /// <summary>Apply SCTP data-channel rules.</summary>
    DataChannel = 8,

    /// <summary>Apply Binary Floor Control Protocol rules.</summary>
    Bfcp = 9,

    /// <summary>Apply all supported strict validation profiles.</summary>
    CombinedStrict = 10
}
