namespace asn1;

internal class EcnClauseSyntax
{
    internal EcnClauseKind Kind;
    internal string Name = "";
    internal AsnToken[] Tokens = [];
    internal string[] Paths = [];
    internal EcnMappingEntrySyntax[] MappingEntries = [];
    internal EcnTokenGroupSyntax[] Items = [];
    internal AsnSourceSpan? Span;
}
