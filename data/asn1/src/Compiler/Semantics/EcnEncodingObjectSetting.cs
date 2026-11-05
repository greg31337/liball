namespace asn1;

internal class EcnEncodingObjectSetting
{
    internal string Name = "";
    internal AsnToken[] Tokens = [];
    internal AsnToken[] ValueTokens = [];
    internal AsnToken[] DefaultTokens = [];
    internal bool Defaulted;
    internal AsnSourceSpan? Span;
}
