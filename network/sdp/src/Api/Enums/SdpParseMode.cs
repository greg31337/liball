namespace sdp;

/// <summary>
/// Specifies how strictly SDP input is parsed and preserved.
/// </summary>
public enum SdpParseMode
{
    /// <summary>The mode is unknown or was not specified.</summary>
    Unknown = 0,

    /// <summary>Accept only input that conforms to the supported SDP grammar.</summary>
    Strict = 1,

    /// <summary>Accept catalogued interoperability deviations and report them as diagnostics.</summary>
    Interoperable = 2,

    /// <summary>Retain original input details and recoverable malformed content.</summary>
    Lossless = 3
}
