namespace asn1;

internal class ParsedParameterizedReferenceSyntax
{
    internal AsnParameterKind Kind;
    internal string ReferenceModule = "";
    internal string ReferenceName = "";
    internal ParsedActualParameterSyntax[] ActualParameterSyntax = [];
    internal AsnActualParameter[] ActualParameters = [];
    internal AsnToken[] SourceTokens = [];
    internal SymbolTableEntry? Target;
    internal ParameterSubstitutionResult? Substitution;
    internal ParameterInstantiationResult? Instantiation;
    internal AsnSourceSpan? Span;
    internal bool RecursionTemplateReference;
    internal bool RecursionActualsFinite;
    internal bool RecursionOptionalBoundary;
    internal bool RecursionChoiceBoundary;
    internal bool Validated;
}
