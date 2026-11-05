namespace asn1;

internal class EcnSymbol
{
    internal string Name = "";
    internal string QualifiedName = "";
    internal string SourceModuleName = "";
    internal EcnSymbolKind Kind;
    internal EcnParameterKind ParameterKind;
    internal EcnGovernorKind GovernorKind;
    internal EcnClassCategory ClassCategory;
    internal EcnClassCategoryGroup[] ClassCategoryGroups = [];
    internal string GovernorName = "";
    internal string PrimitiveClassName = "";
    internal int DeclarationOrder;
    internal int CategoryResolutionState;
    internal bool Exported;
    internal bool Imported;
    internal bool ImportedAsnType;
    internal bool GovernorBuiltIn;
    internal bool BindingFailed;
    internal EcnAssignmentSyntax? Assignment;
    internal EcnParameterSyntax? Parameter;
    internal EcnSymbol? ImportedTarget;
    internal EcnSymbol? GovernorSymbol;
    internal EcnEncodingClassDefinition? ClassDefinition;
    internal EcnEncodingObjectDefinition? ObjectDefinition;
    internal EcnEncodingObjectSetDefinition? ObjectSetDefinition;
    internal AsnAssignment? AsnTypeAssignment;
    internal EcnScope? Scope;
    internal EcnScope? DeclaringScope;
    internal AsnSourceSpan? Span;
}
