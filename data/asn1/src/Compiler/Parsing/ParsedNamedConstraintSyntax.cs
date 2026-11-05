namespace asn1;

internal class ParsedNamedConstraintSyntax
{
    internal string Name = "";
    internal string Presence = "";
    internal AsnConstraintElement Element = new AsnConstraintElement();
    internal ParsedConstraintSyntax? ValueConstraintSyntax;
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
}
