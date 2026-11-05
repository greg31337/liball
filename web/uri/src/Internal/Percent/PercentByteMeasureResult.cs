namespace uri;

internal class PercentByteMeasureResult
{
    internal bool Success;
    internal int Length;
    internal UriDiagnostic? Diagnostic;

    internal PercentByteMeasureResult(bool success, int length, UriDiagnostic? diagnostic)
    {
        Success = success;
        Length = length;
        Diagnostic = diagnostic;
    }
}
