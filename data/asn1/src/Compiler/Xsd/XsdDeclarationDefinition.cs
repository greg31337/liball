namespace asn1;

internal class XsdDeclarationDefinition
{
    internal string StableId = "";
    internal string SourceComponentId = "";
    internal string Kind = "";
    internal string Name = "";
    internal string Identifier = "";
    internal string TypeReferenceName = "";
    internal string NamespaceUri = "";
    internal string SchemaNamespaceUri = "";
    internal bool Qualified;
    internal bool Global;
    internal bool Reference;
    internal string ReferenceName = "";
    internal string ReferenceNamespace = "";
    internal bool Ignored;
    internal XsdDeclarationTypeUse? TypeUse;
    internal System.Numerics.BigInteger MinimumOccurs;
    internal System.Numerics.BigInteger MaximumOccurs;
    internal bool MaximumOccursUnbounded;
    internal bool Optional;
    internal bool Repeated;
    internal string AttributeUse = "";
    internal bool Required;
    internal bool Prohibited;
    internal bool Nillable;
    internal bool Abstract;
    internal bool HasDefault;
    internal string DefaultValue = "";
    internal bool HasFixed;
    internal string FixedValue = "";
    internal string[] BlockValues = [];
    internal string[] FinalValues = [];
    internal XsdValueConstraintDefinition? ValueConstraint;
    internal XsdSubstitutionGroupDefinition? SubstitutionGroup;
    internal XsdTypeSubstitutionDefinition? TypeSubstitution;
    internal string AsnTypeDefinition = "";
    internal string OccurrenceTypeDefinition = "";
    internal AsnEncodingInstruction[] XerInstructions = [];
    internal XsdDocumentInstance? DocumentInstance;
    internal XsdLoadedDocument? Document;
    internal XerXmlToken? Token;
    internal AsnSourceSpan? Span;
}
