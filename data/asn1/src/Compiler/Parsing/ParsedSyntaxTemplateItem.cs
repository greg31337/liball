namespace asn1;

internal class ParsedSyntaxTemplateItem
{
    internal string Text = "";
    internal bool FieldReference;
    internal bool OptionalGroup;
    internal ParsedSyntaxTemplateItem[] Children = [];
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
}
