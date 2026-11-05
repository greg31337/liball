namespace asn1;

internal class EcnTransformPipeline
{
    internal EcnTransformDirection Direction;
    internal EcnTransformValueKind SourceKind;
    internal EcnTransformValueKind ResultKind;
    internal EcnTransformStep[] Steps = [];
    internal int Order;
    internal bool Reversible;
    internal bool Valid;
    internal AsnToken[] Tokens = [];
    internal AsnSourceSpan? Span;
}
