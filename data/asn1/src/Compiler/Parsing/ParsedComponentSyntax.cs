namespace asn1;

internal class ParsedComponentSyntax
{
    internal AsnComponent? Component;
    internal ParsedTypeSyntax? TypeSyntax;
    internal ParsedValueSyntax? DefaultValueSyntax;
    internal bool ComponentsOf;
    internal bool Extension;
    internal bool AfterExtensionEnd;
    internal int ExtensionGroupIndex = -1;
    internal bool HasVersionNumber;
    internal System.Numerics.BigInteger VersionNumber;
    internal AsnToken[] DefaultValueTokens = [];
    internal AsnSourceSpan? Span;
}
