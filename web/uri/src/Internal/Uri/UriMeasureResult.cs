namespace uri;

internal class UriMeasureResult
{
    internal bool Success;
    internal int Length;
    internal UriDiagnostic? Diagnostic;

    internal UriMeasureResult(bool success, int length, UriDiagnostic? diagnostic)
    {
        Success = success;
        Length = length;
        Diagnostic = diagnostic;
    }
}
