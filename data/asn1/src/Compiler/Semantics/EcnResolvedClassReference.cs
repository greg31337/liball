namespace asn1;

internal class EcnResolvedClassReference
{
    internal string Name = "";
    internal string PrimitiveClassName = "";
    internal EcnClassCategory Category;
    internal EcnSymbol? Symbol;
    internal EcnActualParameter[] ActualParameters = [];
    internal bool BuiltIn;
    internal bool Success;
    internal AsnSourceSpan? Span;
}
