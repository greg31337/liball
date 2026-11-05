namespace svg;

/// <summary>Defines recovery behavior for standalone value parsing.</summary>
public enum SvgValueRecoveryMode
{
    /// <summary>Discard a successfully parsed prefix when the complete value is invalid.</summary>
    Strict = 0,
    /// <summary>Retain a successfully parsed prefix while still reporting the exact failure.</summary>
    KeepValidPrefix = 1
}

/// <summary>Defines separators for standalone numeric list grammars.</summary>
public enum SvgValueListSeparator
{
    /// <summary>Accept a comma with optional whitespace or one or more whitespace characters.</summary>
    CommaOrWhitespace = 0,
    /// <summary>Require commas and permit whitespace around them.</summary>
    Comma = 1,
    /// <summary>Require one or more whitespace characters.</summary>
    Whitespace = 2,
    /// <summary>Require semicolons and permit whitespace around them.</summary>
    Semicolon = 3
}

/// <summary>Defines stable standalone value parsing failures.</summary>
public enum SvgValueParseError
{
    /// <summary>No parsing failure occurred.</summary>
    None = 0,
    /// <summary>An input, option, limit, or enum value was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A finite number was required.</summary>
    ExpectedNumber = 2,
    /// <summary>A signed 32-bit decimal integer was required.</summary>
    ExpectedInteger = 3,
    /// <summary>An exponent marker was not followed by exponent digits.</summary>
    InvalidExponent = 4,
    /// <summary>A numeric token exceeded the configured character limit.</summary>
    TokenTooLong = 5,
    /// <summary>A number was non-finite or an integer exceeded the signed 32-bit range.</summary>
    Overflow = 6,
    /// <summary>Characters remained after a complete standalone value.</summary>
    TrailingCharacters = 7,
    /// <summary>A list item was required at the reported offset.</summary>
    ExpectedItem = 8,
    /// <summary>A list item was malformed.</summary>
    InvalidItem = 9,
    /// <summary>The configured list separator was missing.</summary>
    MissingSeparator = 10,
    /// <summary>An explicit separator was not followed by an item.</summary>
    TrailingSeparator = 11,
    /// <summary>The configured item or point count was exceeded.</summary>
    ItemLimitExceeded = 12,
    /// <summary>A points list ended with an unpaired coordinate.</summary>
    OddCoordinateCount = 13
}

/// <summary>Defines deterministic finite-number output modes.</summary>
public enum SvgStandaloneValueFormatMode
{
    /// <summary>Use the runtime's shortest round-trip digits with normalized exponent spelling.</summary>
    ShortestRoundTrip = 0,
    /// <summary>Use canonical SVG decimal and scientific thresholds.</summary>
    Canonical = 1,
    /// <summary>Use the shortest valid plain or exponent SVG token.</summary>
    Compact = 2
}

/// <summary>Defines stable standalone value formatting failures.</summary>
public enum SvgValueFormatError
{
    /// <summary>No formatting failure occurred.</summary>
    None = 0,
    /// <summary>An input collection, item, or enum value was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A number was NaN or infinity.</summary>
    NonFinite = 2,
    /// <summary>The caller-provided destination was too small.</summary>
    DestinationTooSmall = 3
}
