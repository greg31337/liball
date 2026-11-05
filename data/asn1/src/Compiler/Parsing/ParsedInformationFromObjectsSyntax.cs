namespace asn1;

internal class ParsedInformationFromObjectsSyntax
{
    internal string ReferenceModule = "";
    internal string ReferenceName = "";
    internal bool ReferencedObjectSet;
    internal string[] FieldPath = [];
    internal AsnAssignmentKind[] CandidateKinds = [];
    internal ResolvedTypeSyntax? ResolvedType;
    internal AsnType? ResolvedValueSetType;
    internal ResolvedValueSyntax? ResolvedValue;
    internal ParsedInformationObjectSyntax? ResolvedObjectSyntax;
    internal ParsedInformationObjectSetSyntax? ResolvedObjectSetSyntax;
    internal int ResolutionState;
    internal bool ResolutionSuccess;
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
}
