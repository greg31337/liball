namespace asn1;

internal class AssignmentHeadSyntax
{
    internal AssignmentHeadAlternativeSyntax[] Alternatives = [];
    internal AssignmentHeadAlternativeSyntax? DeclaredReference;
    internal AsnToken? AssignmentToken;
    internal bool Ambiguous;
    internal AsnSourceSpan? Span;
}
