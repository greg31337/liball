namespace asn1;

internal class ResolvedValueSyntax
{
    internal ParsedValueSyntax? SourceSyntax;
    internal AsnValue? SourceValue;
    internal AsnValue? EffectiveValue;
    internal ResolvedTypeSyntax? GoverningType;
    internal SymbolTableEntry? SourceDeclaration;
    internal SymbolTableEntry? EffectiveDeclaration;
    internal ResolvedValueSyntax? ReferenceTarget;
    internal ResolvedValueSyntax[] Items = [];
    internal ResolvedComponentValueSyntax[] ComponentValues = [];
    internal string[] ItemNames = [];
    internal bool NamedCollection;
    internal AsnBitStringValue? BitStringValue;
    internal AsnObjectIdentifierValue? ObjectIdentifierValue;
    internal byte[] Octets = [];
    internal bool Success;
}
