namespace uri;

internal class UrnNamespaceSpecificStringParseResult
{
    internal bool Success;
    internal string NamespaceSpecificString;
    internal int NextOffset;
    internal UriDiagnostic? Diagnostic;

    internal UrnNamespaceSpecificStringParseResult(
        bool success,
        string namespaceSpecificString,
        int nextOffset,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        NamespaceSpecificString = namespaceSpecificString;
        NextOffset = nextOffset;
        Diagnostic = diagnostic;
    }
}
