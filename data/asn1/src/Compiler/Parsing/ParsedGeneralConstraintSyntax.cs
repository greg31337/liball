namespace asn1;

internal class ParsedGeneralConstraintSyntax
{
    internal AsnConstraintElement Element = new AsnConstraintElement();
    internal AsnToken[] BodyTokens = [];
    internal AsnToken[][] Parameters = [];
    internal AsnToken[] ObjectSetTokens = [];
    internal ParsedAtNotationSyntax[] AtNotations = [];
    internal ParsedTypeSyntax? ContainedTypeSyntax;
    internal ParsedValueSyntax? EncodingValueSyntax;
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
}
