namespace uri;

internal class UrnPrefixParseResult
{
    internal bool Success;
    internal string Scheme;
    internal string NamespaceIdentifier;
    internal int NextOffset;
    internal UriDiagnostic? Diagnostic;

    internal UrnPrefixParseResult(
        bool success,
        string scheme,
        string namespaceIdentifier,
        int nextOffset,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Scheme = scheme;
        NamespaceIdentifier = namespaceIdentifier;
        NextOffset = nextOffset;
        Diagnostic = diagnostic;
    }
}
