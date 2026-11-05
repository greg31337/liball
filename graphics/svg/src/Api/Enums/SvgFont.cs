namespace svg;

/// <summary>Defines stable SFNT and OpenType parsing failures.</summary>
public enum SvgOpenTypeError
{
    /// <summary>No parsing failure occurred.</summary>
    None = 0,
    /// <summary>The input bytes or options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A configured font byte, table, or glyph bound was exceeded.</summary>
    LimitExceeded = 2,
    /// <summary>The SFNT version or a required table version was unsupported.</summary>
    UnsupportedVersion = 3,
    /// <summary>The table directory was truncated or structurally invalid.</summary>
    InvalidDirectory = 4,
    /// <summary>A required OpenType table was absent.</summary>
    MissingRequiredTable = 5,
    /// <summary>A table checksum or the whole-font checksum was invalid.</summary>
    InvalidChecksum = 6,
    /// <summary>A required metadata, metric, naming, or character-map table was malformed.</summary>
    InvalidTable = 7,
    /// <summary>Parsing was cancelled.</summary>
    Cancelled = 8,
    /// <summary>A WOFF or WOFF2 container was malformed.</summary>
    InvalidContainer = 9,
    /// <summary>A WOFF or WOFF2 compressed data stream could not be decoded.</summary>
    DecompressionFailed = 10,
    /// <summary>A valid web-font collection or table transform requires caller fallback.</summary>
    UnsupportedTransform = 11
}

/// <summary>Identifies the outline table family declared by an SFNT font.</summary>
public enum SvgOpenTypeOutlineKind
{
    /// <summary>No supported outline table family was identified.</summary>
    Unknown = 0,
    /// <summary>TrueType outlines use the <c>glyf</c> and <c>loca</c> tables.</summary>
    TrueType = 1,
    /// <summary>Compact Font Format outlines use the <c>CFF </c> table.</summary>
    Cff = 2,
    /// <summary>Compact Font Format 2 outlines use the <c>CFF2</c> table.</summary>
    Cff2 = 3
}

/// <summary>Defines stable Unicode-to-glyph mapping failures.</summary>
public enum SvgOpenTypeMappingError
{
    /// <summary>No mapping failure occurred.</summary>
    None = 0,
    /// <summary>The font or Unicode scalar input was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The retained character-map data was malformed.</summary>
    InvalidCharacterMap = 2
}

/// <summary>Defines stable glyph-metric lookup and scaling failures.</summary>
public enum SvgOpenTypeMetricError
{
    /// <summary>No metric failure occurred.</summary>
    None = 0,
    /// <summary>The font, glyph index, or requested em size was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The parsed font model does not contain consistent required metrics.</summary>
    InvalidMetrics = 2,
    /// <summary>A scaled result was not finite.</summary>
    NonFiniteResult = 3
}
