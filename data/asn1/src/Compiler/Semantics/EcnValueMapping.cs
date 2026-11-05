namespace asn1;

internal class EcnValueMapping
{
    internal EcnMappingKind Kind;
    internal string Qualifier = "";
    internal EcnValueMappingEntry[] Entries = [];
    internal bool Automatic;
    internal bool Reversible;
    internal bool Valid;
    internal AsnToken[] Tokens = [];
    internal AsnSourceSpan? Span;
}
