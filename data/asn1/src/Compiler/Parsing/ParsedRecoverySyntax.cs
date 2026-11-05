namespace asn1;

internal class ParsedRecoverySyntax
{
    internal string Boundary = "";
    internal AsnToken[] SkippedTokens = [];
    internal AsnSourceSpan? Span;
}
