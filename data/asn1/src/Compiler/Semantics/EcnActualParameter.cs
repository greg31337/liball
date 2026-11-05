namespace asn1;

internal class EcnActualParameter
{
    internal EcnParameterKind ExpectedKind;
    internal string ReferenceName = "";
    internal AsnToken[] Tokens = [];
    internal EcnSymbol? Symbol;
    internal EcnPredeclaredEncodingObjectSet? PredeclaredSet;
    internal bool BuiltIn;
    internal bool Valid;
    internal AsnSourceSpan? Span;
}
