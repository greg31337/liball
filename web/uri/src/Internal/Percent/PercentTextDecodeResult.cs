namespace uri;

internal class PercentTextDecodeResult
{
    internal bool Success;
    internal string Text;
    internal bool UsedReplacement;
    internal UriDiagnostic? Diagnostic;

    internal PercentTextDecodeResult(
        bool success,
        string text,
        bool usedReplacement,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Text = text;
        UsedReplacement = usedReplacement;
        Diagnostic = diagnostic;
    }
}
