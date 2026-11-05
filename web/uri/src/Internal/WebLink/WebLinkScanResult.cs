namespace uri;

internal class WebLinkScanResult
{
    internal bool Success;
    internal string Value;
    internal int StartOffset;
    internal int NextOffset;
    internal TextRange Range;
    internal UriDiagnostic? Diagnostic;

    internal WebLinkScanResult(
        bool success,
        string value,
        int startOffset,
        int nextOffset,
        TextRange range,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Value = value;
        StartOffset = startOffset;
        NextOffset = nextOffset;
        Range = range;
        Diagnostic = diagnostic;
    }
}
