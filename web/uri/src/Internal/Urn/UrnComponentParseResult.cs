namespace uri;

internal class UrnComponentParseResult
{
    internal bool Success;
    internal string Value;
    internal int NextOffset;
    internal UriDiagnostic? Diagnostic;

    internal UrnComponentParseResult(
        bool success,
        string value,
        int nextOffset,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Value = value;
        NextOffset = nextOffset;
        Diagnostic = diagnostic;
    }
}
