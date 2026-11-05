namespace uri;

internal class SchemeScanResult
{
    internal bool Success;
    internal bool HasScheme;
    internal int SchemeLength;
    internal int NextOffset;
    internal UriDiagnostic? Diagnostic;

    internal SchemeScanResult(
        bool success,
        bool hasScheme,
        int schemeLength,
        int nextOffset,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        HasScheme = hasScheme;
        SchemeLength = schemeLength;
        NextOffset = nextOffset;
        Diagnostic = diagnostic;
    }
}
