namespace asn1;

internal class InformationSelectionResult
{
    internal ResolvedTypeSyntax? Type;
    internal AsnType? ValueSetType;
    internal ResolvedValueSyntax? Value;
    internal ParsedInformationObjectSyntax? ObjectSyntax;
    internal ParsedInformationObjectSetSyntax? ObjectSetSyntax;
    internal ParsedObjectClassSyntax? ObjectClassSyntax;
    internal AsnObjectClassField? Field;
    internal bool Success;
}
