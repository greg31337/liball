namespace asn1;

internal class XsdSimpleTypeDefinition
{
    internal string StableId = "";
    internal string Name = "";
    internal string NamespaceUri = "";
    internal string BaseTypeName = "";
    internal string BaseTypeNamespace = "";
    internal string BuiltInBaseName = "";
    internal string AsnTypeDefinition = "";
    internal XsdSimpleTypeVariety Variety;
    internal bool Anonymous;
    internal bool Enumerated;
    internal XsdFacetValue[] Facets = [];
    internal XsdFacetValue[] EffectiveFacets = [];
    internal AsnConstraint[] Constraints = [];
    internal XsdEnumerationItem[] EnumerationItems = [];
    internal AsnEncodingInstruction[] XerInstructions = [];
    internal XsdSimpleTypeUse? ItemType;
    internal XsdUnionAlternative[] UnionAlternatives = [];
    internal XsdSimpleTypeDefinition? BaseDefinition;
    internal XsdComponentDefinition? Component;
    internal XsdLoadedDocument? Document;
    internal XerXmlToken? Token;
    internal AsnSourceSpan? Span;
}
