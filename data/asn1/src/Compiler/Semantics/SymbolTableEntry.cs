namespace asn1;

internal class SymbolTableEntry
{
    internal string Name = "";
    internal string QualifiedName = "";
    internal AsnAssignmentKind[] CandidateKinds = [];
    internal AsnToken[] DeclarationTokens = [];
    internal AsnToken[] SemanticDeclarationTokens = [];
    internal AsnToken[] ReferenceTokens = [];
    internal string[] ParameterNames = [];
    internal ParsedParameterSyntax[] FormalParameterSyntax = [];
    internal AsnParameter[] FormalParameters = [];
    internal ParsedParameterizedReferenceSyntax[] ParameterizedReferences = [];
    internal AsnAssignmentKind Kind;
    internal int DeclarationOrder;
    internal bool Exported;
    internal bool Parameterized;
    internal bool Predeclared;
    internal bool KindClassified;
    internal int TypeResolutionState;
    internal int ValueResolutionState;
    internal AssignmentHeadSyntax? Declaration;
    internal AssignmentHeadAlternativeSyntax? DeclaredReference;
    internal ImportExportSymbolSyntax? ExportReference;
    internal ParsedTypeSyntax? DeclaredTypeSyntax;
    internal ResolvedTypeSyntax? ResolvedType;
    internal ResolvedTypeSyntax? GoverningType;
    internal ParsedValueSyntax? DeclaredValueSyntax;
    internal ResolvedValueSyntax? ResolvedValue;
    internal ParsedObjectClassSyntax? DeclaredObjectClassSyntax;
    internal ParsedObjectClassSyntax? ResolvedObjectClassSyntax;
    internal ParsedInformationObjectSyntax? DeclaredObjectSyntax;
    internal ParsedInformationObjectSetSyntax? DeclaredObjectSetSyntax;
    internal AsnType? ResolvedValueSetType;
    internal int ValueSetResolutionState;
    internal int ObjectClassResolutionState;
    internal int ObjectResolutionState;
    internal int ObjectSetResolutionState;
    internal AsnSourceSpan? Span;

    internal AsnToken[] GetSemanticDeclarationTokens()
    {
        return this.SemanticDeclarationTokens.Length == 0
            ? this.DeclarationTokens
            : this.SemanticDeclarationTokens;
    }
}
