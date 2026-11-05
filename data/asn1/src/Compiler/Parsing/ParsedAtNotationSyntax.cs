namespace asn1;

internal class ParsedAtNotationSyntax
{
    internal bool Relative;
    internal int Levels;
    internal string[] ComponentIdentifiers = [];
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
}
