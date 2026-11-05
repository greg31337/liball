namespace asn1;

internal class XsdEnumerationItem
{
    internal string Identifier = "";
    internal string LexicalValue = "";
    internal string CanonicalValue = "";
    internal AsnValue? Value;
    internal AsnSourceSpan? Span;
}
