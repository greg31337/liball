namespace asn1;

internal class ParameterListParseResult
{
    internal bool Success;
    internal ParsedParameterSyntax[] Parameters = [];
    internal ParsedRecoverySyntax[] Recoveries = [];
    internal int ConsumedTokenCount;
}
