namespace asn1;

internal class InnerCodecMetadata
{
    internal ResolvedTypeSyntax? ContainerType;
    internal ResolvedTypeSyntax? ContainedType;
    internal ResolvedValueSyntax? EncodingValue;
    internal AsnObjectIdentifierValue? EncodingIdentifier;
    internal NormalizedConstraintNode? SourceNode;
    internal bool HasEncodingIdentifier;
    internal bool UsesContainerEncodingRules;
    internal bool RequiresOctetAlignment;
}
