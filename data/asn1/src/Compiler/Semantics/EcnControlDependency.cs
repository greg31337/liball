namespace asn1;

internal class EcnControlDependency
{
    internal EcnControlDependencyKind Kind;
    internal EcnDeterminationMode Mode;
    internal string Reference = "";
    internal string AuxiliaryReference = "";
    internal int Order;
    internal bool EncoderWrites;
    internal bool DecoderReads;
    internal bool RequiresPriorValue;
    internal bool Valid;
    internal AsnToken[] Tokens = [];
    internal AsnSourceSpan? Span;
}
