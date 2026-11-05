namespace asn1;

internal class AssignedOidComponentSyntax
{
    internal string Name = "";
    internal bool HasName;
    internal System.Numerics.BigInteger Number = System.Numerics.BigInteger.Zero;
    internal bool HasNumber;
    internal string DefinedValueModule = "";
    internal string DefinedValue = "";
    internal bool HasDefinedValue;
    internal string NumberDefinedValueModule = "";
    internal string NumberDefinedValue = "";
    internal bool HasNumberDefinedValue;
    internal AsnSourceSpan? Span;
}
