namespace svg;

/// <summary>Defines CSS parser entry-point modes.</summary>
public enum SvgCssParseMode
{
    /// <summary>A complete stylesheet.</summary>
    StyleSheet = 0,
    /// <summary>A standalone declaration list.</summary>
    DeclarationList = 1,
    /// <summary>An SVG or HTML style attribute declaration list.</summary>
    StyleAttribute = 2
}

/// <summary>Defines fatal bounded CSS parser failures.</summary>
public enum SvgCssParseError
{
    /// <summary>No fatal parser failure occurred.</summary>
    None = 0,
    /// <summary>An input or configured limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The source character limit was exceeded.</summary>
    CharacterLimitExceeded = 2,
    /// <summary>The lexical token limit was exceeded.</summary>
    TokenLimitExceeded = 3,
    /// <summary>The stylesheet rule limit was exceeded.</summary>
    RuleLimitExceeded = 4,
    /// <summary>The declaration limit was exceeded.</summary>
    DeclarationLimitExceeded = 5,
    /// <summary>The nested component block limit was exceeded.</summary>
    BlockDepthExceeded = 6
}
