namespace asn1;

internal class AssignmentHeadParseResult
{
    internal bool Success;
    internal AssignmentHeadSyntax[] Heads = [];
    internal ParsedRecoverySyntax[] Recoveries = [];
}
