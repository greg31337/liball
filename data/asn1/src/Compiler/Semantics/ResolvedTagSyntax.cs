namespace asn1;

internal class ResolvedTagSyntax
{
    internal AsnTag Tag = new AsnTag();
    internal ParsedTypeSyntax? OriginSyntax;
    internal ParsedComponentSyntax? OriginComponentSyntax;
    internal SymbolTableEntry? OriginDeclaration;
    internal ModuleSyntax? OriginModule;
    internal AsnTypeKind UniversalTypeKind;
    internal AsnTaggingMode DeclaredMode;
    internal string NumberReference = "";
    internal bool Universal;
    internal bool DefaultModeApplied;
    internal bool HasNumericNumber;
    internal bool AutomaticallyGenerated;
    internal int AutomaticIndex = -1;
}
