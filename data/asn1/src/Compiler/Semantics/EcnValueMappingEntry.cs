namespace asn1;

internal class EcnValueMappingEntry
{
    internal string Source = "";
    internal string Target = "";
    internal int Order;
    internal bool Remainder;
    internal bool Valid;
    internal AsnToken[] SourceTokens = [];
    internal AsnToken[] TargetTokens = [];
    internal AsnSourceSpan? Span;
}
