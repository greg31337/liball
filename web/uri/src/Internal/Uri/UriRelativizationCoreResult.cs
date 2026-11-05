namespace uri;

internal class UriRelativizationCoreResult
{
    internal bool Success;
    internal UriReference Reference;
    internal string Text;
    internal int Length;
    internal UriDiagnostic? Diagnostic;

    internal UriRelativizationCoreResult(
        bool success,
        UriReference reference,
        string text,
        int length,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Reference = reference;
        Text = text;
        Length = length;
        Diagnostic = diagnostic;
    }
}
