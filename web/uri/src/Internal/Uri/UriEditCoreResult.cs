namespace uri;

internal class UriEditCoreResult
{
    internal bool Success;
    internal UriReference Reference;
    internal string Text;
    internal int Length;
    internal UriDiagnostic? Diagnostic;

    internal UriEditCoreResult(
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
