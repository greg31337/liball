namespace asn1;

internal class CompiledSyntaxItem
{
    internal string Literal = "";
    internal AsnObjectClassField? Field;
    internal bool OptionalGroup;
    internal CompiledSyntaxItem[] Children = [];
    internal CompiledSyntaxItem[] FirstItems = [];
    internal AsnSourceSpan? Span;
}
