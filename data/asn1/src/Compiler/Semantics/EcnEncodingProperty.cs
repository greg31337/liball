namespace asn1;

internal class EcnEncodingProperty
{
    internal EcnEncodingPropertyKind Kind;
    internal EcnEncodingPropertyScope Scope;
    internal EcnEncodingPropertyValueKind ValueKind;
    internal string Name = "";
    internal string Value = "";
    internal string Unit = "";
    internal string LowerBound = "";
    internal string UpperBound = "";
    internal AsnToken[] Tokens = [];
    internal AsnToken[] ValueTokens = [];
    internal long IntegerValue;
    internal bool HasIntegerValue;
    internal bool Valid;
    internal AsnSourceSpan? Span;
}
