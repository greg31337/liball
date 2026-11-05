namespace asn1;

internal class EcnConditionDefinition
{
    internal string Name = "";
    internal string Expression = "";
    internal string Reference = "";
    internal string Comparison = "";
    internal string Value = "";
    internal int Order;
    internal bool All;
    internal bool Valid;
    internal AsnToken[] Tokens = [];
    internal AsnSourceSpan? Span;
}
