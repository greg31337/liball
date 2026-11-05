namespace asn1;

internal class AssignmentHeadAlternativeSyntax
{
    internal string Name = "";
    internal AsnAssignmentKind[] CandidateKinds = [];
    internal AsnToken[] GovernorTokens = [];
    internal ParsedTypeSyntax? GovernorType;
    internal AsnSourceSpan? Span;
}
