namespace asn1;

internal class ParsedActualParameterSyntax
{
    internal AsnActualParameter Parameter = new AsnActualParameter();
    internal AsnParameterKind[] CandidateKinds = [];
    internal ParsedTypeSyntax? TypeSyntax;
    internal ParsedValueSyntax? ValueSyntax;
    internal ParsedConstraintSyntax? ValueSetSyntax;
    internal ParsedObjectClassSyntax? ObjectClassSyntax;
    internal ParsedInformationObjectSyntax? ObjectSyntax;
    internal ParsedInformationObjectSetSyntax? ObjectSetSyntax;
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
}
