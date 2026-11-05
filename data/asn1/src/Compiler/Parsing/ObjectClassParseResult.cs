namespace asn1;

internal class ObjectClassParseResult
{
    internal bool Success;
    internal ParsedObjectClassSyntax? Syntax;
    internal int ConsumedTokenCount;
}
