namespace uri;

internal class PercentNormalizeResult
{
    internal bool Success;
    internal string Text;
    internal UriDiagnostic? Diagnostic;

    internal PercentNormalizeResult(bool success, string text, UriDiagnostic? diagnostic)
    {
        Success = success;
        Text = text;
        Diagnostic = diagnostic;
    }
}
