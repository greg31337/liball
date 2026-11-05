namespace asn1;

internal class EcnParameterSyntax
{
    internal string Governor = "";
    internal string Reference = "";
    internal AsnToken[] Tokens = [];
    internal EcnSymbol? Symbol;
    internal AsnSourceSpan? Span;
}
