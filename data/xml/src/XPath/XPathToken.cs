namespace xml;

internal enum XPathTokenKind
{
    EndOfInput = 0,
    NcName = 1,
    QName = 2,
    UriQualifiedName = 3,
    BracedUriLiteral = 4,
    Keyword = 5,
    IntegerLiteral = 6,
    DecimalLiteral = 7,
    DoubleLiteral = 8,
    StringLiteral = 9,
    Bang = 10,
    NotEqual = 11,
    Hash = 12,
    Dollar = 13,
    LeftParenthesis = 14,
    RightParenthesis = 15,
    Star = 16,
    StarColon = 17,
    Plus = 18,
    Comma = 19,
    Minus = 20,
    Dot = 21,
    DotDot = 22,
    Slash = 23,
    SlashSlash = 24,
    Colon = 25,
    ColonStar = 26,
    ColonColon = 27,
    ColonEquals = 28,
    LessThan = 29,
    NodeBefore = 30,
    LessThanOrEqual = 31,
    Equals = 32,
    Arrow = 33,
    GreaterThan = 34,
    GreaterThanOrEqual = 35,
    NodeAfter = 36,
    Question = 37,
    At = 38,
    LeftBracket = 39,
    RightBracket = 40,
    LeftBrace = 41,
    Pipe = 42,
    PipePipe = 43,
    RightBrace = 44
}

internal class XPathToken
{
    internal XPathTokenKind Kind;
    internal string Lexeme;
    internal string? Value;
    internal string? Prefix;
    internal string? LocalName;
    internal string? NamespaceUri;
    internal char Quote;
    internal bool HasLeadingSeparator;
    internal int StartUtf16Offset;
    internal int Utf16Length;
    internal SourceRange Range;

    internal XPathToken(XPathTokenKind kind, string lexeme, string? value, string? prefix, string? localName, string? namespaceUri, char quote, bool hasLeadingSeparator, int startUtf16Offset, int utf16Length, SourceRange range)
    {
        Kind = kind;
        Lexeme = lexeme;
        Value = value;
        Prefix = prefix;
        LocalName = localName;
        NamespaceUri = namespaceUri;
        Quote = quote;
        HasLeadingSeparator = hasLeadingSeparator;
        StartUtf16Offset = startUtf16Offset;
        Utf16Length = utf16Length;
        Range = range;
    }
}
