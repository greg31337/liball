namespace uri;

internal class UriCompositionResult
{
    internal bool Success;
    internal string Text;
    internal int Length;
    internal UriDiagnostic? Diagnostic;

    internal UriCompositionResult(
        bool success,
        string text,
        int length,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Text = text;
        Length = length;
        Diagnostic = diagnostic;
    }
}
