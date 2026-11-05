namespace uri;

internal class QueryFragmentScanResult
{
    internal bool Success;
    internal bool Present;
    internal int ValueOffset;
    internal int ValueLength;
    internal int NextOffset;
    internal UriDiagnostic? Diagnostic;

    internal QueryFragmentScanResult(
        bool success,
        bool present,
        int valueOffset,
        int valueLength,
        int nextOffset,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Present = present;
        ValueOffset = valueOffset;
        ValueLength = valueLength;
        NextOffset = nextOffset;
        Diagnostic = diagnostic;
    }
}
