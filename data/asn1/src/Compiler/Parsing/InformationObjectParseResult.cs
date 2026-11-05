namespace asn1;

internal class InformationObjectParseResult
{
    internal bool Success;
    internal ParsedInformationObjectSyntax? Syntax;
    internal int ConsumedTokenCount;
}
