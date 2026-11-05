namespace asn1;

internal class ParsedObjectClassFieldSyntax
{
    internal AsnObjectClassField Field = new AsnObjectClassField();
    internal ParsedTypeSyntax? GovernorTypeSyntax;
    internal string[] GovernorFieldPath = [];
    internal string GovernorObjectClassModule = "";
    internal string GovernorObjectClassName = "";
    internal ParsedTypeSyntax? DefaultTypeSyntax;
    internal ParsedValueSyntax? DefaultValueSyntax;
    internal AsnToken[] DefaultTokens = [];
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
}
