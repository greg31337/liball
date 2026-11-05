namespace asn1;

internal class XsdModelGroupDefinition
{
    internal string StableId = "";
    internal string Name = "";
    internal string NamespaceUri = "";
    internal string Kind = "";
    internal string TypeReferenceName = "";
    internal bool AttributeGroup;
    internal bool Mapped;
    internal XsdParticleDefinition? Particle;
    internal XsdDeclarationDefinition[] AttributeUses = [];
    internal XsdWildcardDefinition? AttributeWildcard;
    internal string[] ReferencedAttributeGroups = [];
    internal string AsnTypeDefinition = "";
    internal AsnEncodingInstruction[] XerInstructions = [];
    internal XsdComponentDefinition? Component;
    internal XsdDocumentInstance? DocumentInstance;
    internal XsdLoadedDocument? Document;
    internal XerXmlToken? Token;
    internal AsnSourceSpan? Span;
}
