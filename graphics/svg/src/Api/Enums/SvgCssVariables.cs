namespace svg;

/// <summary>Defines stored custom-property token categories.</summary>
public enum SvgCssStoredTokenKind
{
    /// <summary>CSS whitespace.</summary>
    Whitespace = 0,
    /// <summary>An identifier-like token.</summary>
    Identifier = 1,
    /// <summary>A number, dimension, or percentage-like token.</summary>
    Numeric = 2,
    /// <summary>A quoted string token.</summary>
    String = 3,
    /// <summary>A CSS comment token.</summary>
    Comment = 4,
    /// <summary>A delimiter or bracket token.</summary>
    Delimiter = 5
}

/// <summary>Defines stable custom-property storage and substitution failures.</summary>
public enum SvgCssVariableError
{
    /// <summary>No variable failure occurred.</summary>
    None = 0,
    /// <summary>An input or configured bound was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A custom-property name was malformed.</summary>
    InvalidName = 2,
    /// <summary>A stored token stream was malformed or unbalanced.</summary>
    InvalidTokens = 3,
    /// <summary>A token exceeded its configured character bound.</summary>
    TokenTooLong = 4,
    /// <summary>The configured token count was exceeded.</summary>
    TokenLimitExceeded = 5,
    /// <summary>A referenced custom property was missing and had no fallback.</summary>
    MissingVariable = 6,
    /// <summary>A custom-property dependency cycle was detected.</summary>
    CyclicVariable = 7,
    /// <summary>The configured substitution depth was exceeded.</summary>
    DepthExceeded = 8,
    /// <summary>The configured expansion token or character bound was exceeded.</summary>
    ExpansionLimitExceeded = 9,
    /// <summary>A <c>var()</c> function was malformed.</summary>
    InvalidFunction = 10,
    /// <summary>Substitution produced a value outside the target property's grammar.</summary>
    InvalidComputedValue = 11
}
