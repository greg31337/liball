namespace asn1;

internal class EcnEncodingStructure
{
    internal EcnEncodingStructureKind Kind;
    internal string ClassReference = "";
    internal string PrimitiveClassName = "";
    internal string ElementName = "";
    internal EcnClassCategory ClassCategory;
    internal EcnSymbol? ClassSymbol;
    internal EcnActualParameter[] ActualParameters = [];
    internal EcnEncodingField[] Fields = [];
    internal EcnEncodingStructure? Element;
    internal EcnEncodingStructure? InnerStructure;
    internal AsnToken[] ConstraintTokens = [];
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
}
