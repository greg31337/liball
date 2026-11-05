namespace asn1;

internal class EcnTransformStep
{
    internal string Signature = "";
    internal string Operation = "";
    internal string Argument = "";
    internal string ReferenceName = "";
    internal EcnSymbol? ReferenceSymbol;
    internal EcnTransformValueKind SourceKind;
    internal EcnTransformValueKind ResultKind;
    internal int Order;
    internal bool Inline;
    internal bool Reversible;
    internal bool RequiresDomainValidation;
    internal bool Valid;
    internal AsnToken[] Tokens = [];
    internal AsnSourceSpan? Span;
}
