namespace asn1;

internal class EcnEncodingObjectSetTerm
{
    internal EcnObjectSetOperation Operation;
    internal EcnObjectSetTermKind Kind;
    internal string ReferenceName = "";
    internal EcnSymbol? Symbol;
    internal EcnPredeclaredEncodingObjectSet? PredeclaredSet;
    internal EcnActualParameter[] ActualParameters = [];
    internal AsnToken[] Tokens = [];
    internal bool Valid;
    internal AsnSourceSpan? Span;
}
