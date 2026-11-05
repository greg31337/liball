namespace uri;

internal class PercentEncodeResult
{
    internal bool Success;
    internal string Text;
    internal UriDiagnostic? Diagnostic;

    internal PercentEncodeResult(bool success, string text, UriDiagnostic? diagnostic)
    {
        Success = success;
        Text = text;
        Diagnostic = diagnostic;
    }
}
