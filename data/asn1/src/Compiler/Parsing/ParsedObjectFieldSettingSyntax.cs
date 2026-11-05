namespace asn1;

internal class ParsedObjectFieldSettingSyntax
{
    internal AsnObjectFieldSetting Setting = new AsnObjectFieldSetting();
    internal AsnObjectFieldKind Kind;
    internal ParsedTypeSyntax? TypeSyntax;
    internal ResolvedTypeSyntax? TypeResolution;
    internal ParsedValueSyntax? ValueSyntax;
    internal ParsedConstraintSyntax? ValueSetSyntax;
    internal ParsedInformationObjectSyntax? ObjectSyntax;
    internal ParsedInformationObjectSetSyntax? ObjectSetSyntax;
    internal ParsedInformationFromObjectsSyntax? InformationFromObjects;
    internal ParsedParameterizedReferenceSyntax? ParameterizedReference;
    internal AsnToken[] SourceTokens = [];
    internal SymbolTable? ResolutionModule;
    internal AsnSourceSpan? Span;
}
