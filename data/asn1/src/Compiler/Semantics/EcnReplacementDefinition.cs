namespace asn1;

internal class EcnReplacementDefinition
{
    internal EcnReplacementScope Scope;
    internal string ReplacementClassName = "";
    internal EcnSymbol? ReplacementClassSymbol;
    internal string EncodingObjectName = "";
    internal EcnSymbol? EncodingObjectSymbol;
    internal string HeadStructureName = "";
    internal EcnSymbol? HeadStructureSymbol;
    internal bool ReplacementBuiltIn;
    internal bool Secondary;
    internal bool Valid;
    internal AsnToken[] Tokens = [];
    internal AsnSourceSpan? Span;
}
