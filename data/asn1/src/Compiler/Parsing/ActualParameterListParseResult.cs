namespace asn1;

internal class ActualParameterListParseResult
{
    internal bool Success;
    internal ParsedActualParameterSyntax[] Parameters = [];
    internal ParsedRecoverySyntax[] Recoveries = [];
    internal int ConsumedTokenCount;
}
