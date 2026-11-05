namespace uri;

internal class PercentTextMeasureResult
{
    internal bool Success;
    internal long Length;
    internal UriDiagnostic? Diagnostic;

    internal PercentTextMeasureResult(bool success, long length, UriDiagnostic? diagnostic)
    {
        Success = success;
        Length = length;
        Diagnostic = diagnostic;
    }
}
