namespace asn1;

internal class EcnOrderedEncodingObject
{
    internal int Order;
    internal string ReferenceName = "";
    internal AsnToken[] Tokens = [];
    internal EcnSymbol? Symbol;
    internal EcnActualParameter[] ActualParameters = [];
    internal bool Inline;
    internal bool Valid;
    internal AsnSourceSpan? Span;
}
