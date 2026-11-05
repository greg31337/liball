namespace uri;

internal class WebLinkRecoveryFieldResult
{
    internal bool Success;
    internal WebLink[] Links;
    internal UriDiagnostic[] Diagnostics;
    internal UriDiagnostic? Diagnostic;

    internal WebLinkRecoveryFieldResult(
        bool success,
        WebLink[] links,
        UriDiagnostic[] diagnostics,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Links = links;
        Diagnostics = diagnostics;
        Diagnostic = diagnostic;
    }
}
