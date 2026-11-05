namespace asn1;

internal class EcnEncodingLink
{
    internal EcnEncodingLinkKind Kind;
    internal string ReferenceName = "";
    internal EcnSymbol? ReferenceSymbol;
    internal EcnFieldPath? TargetPath;
    internal EcnActualParameter[] ActualParameters = [];
    internal EcnValueMapping? Mapping;
    internal EcnPredeclaredEncodingObjectSet? PredeclaredObjectSet;
    internal int Order;
    internal bool BuiltIn;
    internal bool Valid;
    internal AsnToken[] Tokens = [];
    internal AsnSourceSpan? Span;
}
