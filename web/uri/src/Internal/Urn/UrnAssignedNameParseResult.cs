namespace uri;

internal class UrnAssignedNameParseResult
{
    internal bool Success;
    internal string Scheme;
    internal string NamespaceIdentifier;
    internal string NamespaceSpecificString;
    internal string AssignedName;
    internal int NextOffset;
    internal UriDiagnostic? Diagnostic;

    internal UrnAssignedNameParseResult(
        bool success,
        string scheme,
        string namespaceIdentifier,
        string namespaceSpecificString,
        string assignedName,
        int nextOffset,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Scheme = scheme;
        NamespaceIdentifier = namespaceIdentifier;
        NamespaceSpecificString = namespaceSpecificString;
        AssignedName = assignedName;
        NextOffset = nextOffset;
        Diagnostic = diagnostic;
    }
}
