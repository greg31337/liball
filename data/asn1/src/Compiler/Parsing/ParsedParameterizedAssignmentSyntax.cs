namespace asn1;

internal class ParsedParameterizedAssignmentSyntax
{
    internal AsnAssignment Assignment = new AsnAssignment();
    internal ParsedParameterSyntax[] ParameterSyntax = [];
    internal ParsedTypeSyntax? GovernorTypeSyntax;
    internal ParsedObjectClassSyntax? GovernorObjectClassSyntax;
    internal ParsedTypeSyntax? TypeSyntax;
    internal ParsedValueSyntax? ValueSyntax;
    internal ParsedConstraintSyntax? ValueSetSyntax;
    internal AsnToken[] ValueSetTokens = [];
    internal ParsedObjectClassSyntax? ObjectClassSyntax;
    internal ParsedInformationObjectSyntax? ObjectSyntax;
    internal ParsedInformationObjectSetSyntax? ObjectSetSyntax;
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
}
