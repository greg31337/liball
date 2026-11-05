using System;

namespace svg;

/// <summary>Defines supported SVG paint value kinds.</summary>
public enum SvgPaintKind
{
    /// <summary>No paint is applied.</summary>
    None = 0,

    /// <summary>A solid CSS color is applied.</summary>
    Color = 1,

    /// <summary>A URL references a paint server.</summary>
    Url = 2,

    /// <summary>The context element fill paint is used.</summary>
    ContextFill = 3,

    /// <summary>The context element stroke paint is used.</summary>
    ContextStroke = 4
}

/// <summary>Defines stable SVG paint parsing failures.</summary>
public enum SvgPaintParseError
{
    /// <summary>No parsing failure occurred.</summary>
    None = 0,

    /// <summary>An input or configured bound was invalid.</summary>
    InvalidArgument = 1,

    /// <summary>A paint value was required.</summary>
    ExpectedPaint = 2,

    /// <summary>The paint keyword or value was not recognized.</summary>
    UnknownPaint = 3,

    /// <summary>A color paint was malformed.</summary>
    InvalidColor = 4,

    /// <summary>A URL paint reference was empty or malformed.</summary>
    InvalidUrl = 5,

    /// <summary>A URL or color token exceeded its configured bound.</summary>
    TokenTooLong = 6,

    /// <summary>A fallback was not valid in its chain position.</summary>
    InvalidFallback = 7,

    /// <summary>The configured fallback count was exceeded.</summary>
    FallbackLimitExceeded = 8,

    /// <summary>Characters followed a complete non-URL paint value.</summary>
    TrailingCharacters = 9
}
