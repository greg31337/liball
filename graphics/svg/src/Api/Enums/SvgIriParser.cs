namespace svg;

/// <summary>Defines stable reference and textual microsyntax failures.</summary>
public enum SvgIriParseError
{
    /// <summary>No parsing failure occurred.</summary>
    None = 0,
    /// <summary>An input or configured bound was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A nonempty value was required.</summary>
    ExpectedValue = 2,
    /// <summary>A character was not allowed in the selected grammar.</summary>
    InvalidCharacter = 3,
    /// <summary>A percent escape was incomplete or malformed.</summary>
    InvalidPercentEscape = 4,
    /// <summary>A CSS escape was incomplete or malformed.</summary>
    InvalidEscape = 5,
    /// <summary>A fragment-only reference was required.</summary>
    InvalidFragment = 6,
    /// <summary>A function name or opening parenthesis was malformed.</summary>
    InvalidFunction = 7,
    /// <summary>A closing parenthesis or quote was required.</summary>
    ExpectedClose = 8,
    /// <summary>Characters followed a complete value.</summary>
    TrailingCharacters = 9,
    /// <summary>A token exceeded its configured character bound.</summary>
    TokenTooLong = 10,
    /// <summary>The configured item count was exceeded.</summary>
    ItemLimitExceeded = 11,
    /// <summary>A required list separator or item was missing.</summary>
    InvalidSeparator = 12,
    /// <summary>A media type or parameter was malformed.</summary>
    InvalidMediaType = 13,
    /// <summary>A language tag was malformed.</summary>
    InvalidLanguageTag = 14,
    /// <summary>A feature string was malformed.</summary>
    InvalidFeatureString = 15
}
