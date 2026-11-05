namespace asn1;

internal class TableConstraintRelationship
{
    internal AsnConstraintKind Kind;
    internal ResolvedTypeSyntax? GoverningType;
    internal ResolvedComponentSyntax? ReferencingComponent;
    internal ResolvedTypeSyntax? ConstrainedType;
    internal ResolvedObjectClassFieldSyntax? ConstrainedField;
    internal SymbolTableEntry? ObjectSetSymbol;
    internal AsnInformationObjectSet? ObjectSet;
    internal TableAtNotationResolution[] AtNotations = [];
    internal AsnOpenTypeDispatchEntry[] DispatchEntries = [];
    internal OpenTypeDispatchMap? DispatchMap;
    internal NormalizedConstraintNode? SourceNode;
    internal string StableId = "";
}
