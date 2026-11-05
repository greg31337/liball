namespace iso20022;

internal class GenerationFacet
{
    internal GenerationSourceKind SourceKind;
    internal GenerationConceptKind OwnerKind;
    internal int SourceOrdinal;
    internal int DeclarationOrdinal;
    internal int FacetOrdinal;
    internal string OwnerIdentifier = string.Empty;
    internal DataTypeFacet Facet = new DataTypeFacet();
}
