namespace asn1;

internal class XsdParticleDefinition
{
    internal string StableId = "";
    internal string Kind = "";
    internal string Name = "";
    internal string Identifier = "";
    internal System.Numerics.BigInteger MinimumOccurs;
    internal System.Numerics.BigInteger MaximumOccurs;
    internal bool MaximumOccursUnbounded;
    internal bool Optional;
    internal bool Repeated;
    internal bool PointlessSequence;
    internal bool PendingWildcardMapping;
    internal XsdWildcardDefinition? Wildcard;
    internal XsdDeclarationDefinition? Declaration;
    internal XsdModelGroupDefinition? ReferencedGroup;
    internal XsdParticleDefinition[] Children = [];
    internal AsnConstraint[] Constraints = [];
    internal AsnEncodingInstruction[] XerInstructions = [];
    internal string AsnTypeDefinition = "";
    internal string NamedTypeDefinition = "";
    internal XerXmlToken? Token;
    internal AsnSourceSpan? Span;
}
