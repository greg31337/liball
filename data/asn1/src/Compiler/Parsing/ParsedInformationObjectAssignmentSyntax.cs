namespace asn1;

internal class ParsedInformationObjectAssignmentSyntax
{
    internal AsnAssignment Assignment = new AsnAssignment();
    internal ParsedObjectClassSyntax? GovernorSyntax;
    internal ParsedInformationObjectSyntax? ObjectSyntax;
    internal ParsedInformationObjectSetSyntax? ObjectSetSyntax;
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
}
