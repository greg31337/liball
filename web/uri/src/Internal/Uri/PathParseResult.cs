namespace uri;

internal class PathParseResult
{
    internal bool Success;
    internal int SegmentCount;
    internal UriDiagnostic? Diagnostic;

    internal PathParseResult(
        bool success,
        int segmentCount,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        SegmentCount = segmentCount;
        Diagnostic = diagnostic;
    }
}
