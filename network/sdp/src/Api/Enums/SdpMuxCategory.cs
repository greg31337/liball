namespace sdp;

/// <summary>
/// Identifies the BUNDLE multiplexing category of an SDP attribute.
/// </summary>
public enum SdpMuxCategory
{
    /// <summary>The multiplexing category is unknown or extension-defined.</summary>
    Unknown = 0,

    /// <summary>The attribute may be independently specified for each media description.</summary>
    Normal = 1,

    /// <summary>Multiplexing the attribute requires explicit specification-defined care.</summary>
    Caution = 2,

    /// <summary>The attribute and its value must be identical across bundled media.</summary>
    Identical = 3,

    /// <summary>The bundled value is the sum of the individual media values.</summary>
    Sum = 4,

    /// <summary>The attribute belongs to the shared transport.</summary>
    Transport = 5,

    /// <summary>The value must be identical for each RTP payload type.</summary>
    IdenticalPerPayloadType = 6,

    /// <summary>The defining specification supplies attribute-specific multiplexing rules.</summary>
    Special = 7
}
