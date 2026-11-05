namespace asn1;

internal class ParsedValueSyntax
{
    internal AsnValue Value = new AsnValue();
    internal ParsedValueSyntax[] Items = [];
    internal ParsedValueSyntax? InnerValue;
    internal ParsedTypeSyntax? GoverningTypeSyntax;
    internal ParsedInformationFromObjectsSyntax? InformationFromObjects;
    internal ParsedParameterizedReferenceSyntax? ParameterizedReference;
    internal XmlElementSyntax? XmlElement;
    internal AsnBitStringValue? BitStringValue;
    internal AsnObjectIdentifierValue? ObjectIdentifierValue;
    internal AssignedOidComponentSyntax[] ObjectIdentifierComponents = [];
    internal byte[] Octets = [];
    internal string ReferenceModule = "";
    internal string ReferenceName = "";
    internal bool IntegerExpression;
    internal ResolvedValueSyntax? Resolution;
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
}
