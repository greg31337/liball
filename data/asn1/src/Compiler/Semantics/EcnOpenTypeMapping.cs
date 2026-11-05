namespace asn1;

internal class EcnOpenTypeMapping
{
    internal string KnownEncodingsName = "";
    internal EcnSymbol? KnownEncodingsSymbol;
    internal EcnPredeclaredEncodingObjectSet? KnownPredeclaredEncodings;
    internal string UnknownStructureName = "";
    internal EcnSymbol? UnknownStructureSymbol;
    internal string UnknownEncodingsName = "";
    internal EcnSymbol? UnknownEncodingsSymbol;
    internal EcnPredeclaredEncodingObjectSet? UnknownPredeclaredEncodings;
    internal bool KnownEncodingsBuiltIn;
    internal bool UnknownEncodingsBuiltIn;
    internal bool Valid;
    internal AsnSourceSpan? Span;
}
