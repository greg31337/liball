namespace svg;

/// <summary>Defines CSS Syntax lexical token kinds.</summary>
public enum SvgCssTokenKind
{
    /// <summary>A run of CSS whitespace.</summary>
    Whitespace = 0,
    /// <summary>A source-preserved CSS comment.</summary>
    Comment = 1,
    /// <summary>An identifier token.</summary>
    Ident = 2,
    /// <summary>A function token including its opening parenthesis.</summary>
    Function = 3,
    /// <summary>An at-keyword token.</summary>
    AtKeyword = 4,
    /// <summary>A hash token.</summary>
    Hash = 5,
    /// <summary>A quoted string token.</summary>
    String = 6,
    /// <summary>A string terminated by an unescaped newline.</summary>
    BadString = 7,
    /// <summary>An unquoted URL token.</summary>
    Url = 8,
    /// <summary>A malformed unquoted URL token.</summary>
    BadUrl = 9,
    /// <summary>A number token.</summary>
    Number = 10,
    /// <summary>A percentage token.</summary>
    Percentage = 11,
    /// <summary>A dimension token.</summary>
    Dimension = 12,
    /// <summary>A Unicode range token.</summary>
    UnicodeRange = 13,
    /// <summary>A single unmatched delimiter.</summary>
    Delimiter = 14,
    /// <summary>The CSS CDO sequence <c>&lt;!--</c>.</summary>
    Cdo = 15,
    /// <summary>The CSS CDC sequence <c>--&gt;</c>.</summary>
    Cdc = 16,
    /// <summary>A colon.</summary>
    Colon = 17,
    /// <summary>A semicolon.</summary>
    Semicolon = 18,
    /// <summary>A comma.</summary>
    Comma = 19,
    /// <summary>An opening square bracket.</summary>
    OpenSquare = 20,
    /// <summary>A closing square bracket.</summary>
    CloseSquare = 21,
    /// <summary>An opening parenthesis not consumed by a function token.</summary>
    OpenParenthesis = 22,
    /// <summary>A closing parenthesis.</summary>
    CloseParenthesis = 23,
    /// <summary>An opening curly bracket.</summary>
    OpenCurly = 24,
    /// <summary>A closing curly bracket.</summary>
    CloseCurly = 25
}

/// <summary>Defines the lexical subtype of a CSS number.</summary>
public enum SvgCssNumberType
{
    /// <summary>The authored representation has no fraction or exponent.</summary>
    Integer = 0,
    /// <summary>The authored representation has a fraction or exponent.</summary>
    Number = 1
}

/// <summary>Defines the contextual subtype of a CSS hash token.</summary>
public enum SvgCssHashType
{
    /// <summary>The hash payload is unrestricted.</summary>
    Unrestricted = 0,
    /// <summary>The hash payload would start an identifier.</summary>
    Id = 1
}

/// <summary>Defines bounded CSS tokenizer failures.</summary>
public enum SvgCssTokenizeError
{
    /// <summary>No tokenizer failure occurred.</summary>
    None = 0,
    /// <summary>An argument or configured limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The configured source character limit was exceeded.</summary>
    CharacterLimitExceeded = 2,
    /// <summary>The configured token count was exceeded.</summary>
    TokenLimitExceeded = 3
}
