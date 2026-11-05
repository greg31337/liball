namespace asn1;

internal class XsdComplexTypeDefinition
{
    internal string StableId = "";
    internal string Name = "";
    internal string NamespaceUri = "";
    internal string TypeReferenceName = "";
    internal bool Anonymous;
    internal bool Abstract;
    internal bool Mixed;
    internal bool Mapped;
    internal string ContentKind = "";
    internal string Derivation = "";
    internal string BaseTypeName = "";
    internal string BaseTypeNamespace = "";
    internal XsdComplexTypeDefinition? BaseComplexType;
    internal XsdSimpleTypeUse? SimpleContentType;
    internal XsdParticleDefinition? Particle;
    internal XsdWildcardDefinition? AttributeWildcard;
    internal XsdWildcardDefinition[] Wildcards = [];
    internal XsdDeclarationDefinition[] AttributeUses = [];
    internal string[] AttributeGroupReferences = [];
    internal AsnEncodingInstruction[] XerInstructions = [];
    internal string AsnTypeDefinition = "";
    internal XsdComponentDefinition? Component;
    internal XsdDocumentInstance? DocumentInstance;
    internal XsdLoadedDocument? Document;
    internal XerXmlToken? Token;
    internal AsnSourceSpan? Span;
}
