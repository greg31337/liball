namespace uri;

internal class UriQueryComposeCoreResult
{
    internal bool Success;
    internal string Text;
    internal int Length;
    internal char PairDelimiter;
    internal UriDiagnostic? Diagnostic;

    internal UriQueryComposeCoreResult(
        bool success,
        string text,
        int length,
        char pairDelimiter,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Text = text;
        Length = length;
        PairDelimiter = pairDelimiter;
        Diagnostic = diagnostic;
    }
}
