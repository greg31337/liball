namespace uri;

internal class WebLinkRecoveryLinkResult
{
    internal bool Success;
    internal bool Included;
    internal WebLink Link;
    internal int NextOffset;
    internal UriDiagnostic[] Diagnostics;
    internal UriDiagnostic? Diagnostic;

    internal WebLinkRecoveryLinkResult(
        bool success,
        bool included,
        WebLink link,
        int nextOffset,
        UriDiagnostic[] diagnostics,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Included = included;
        Link = link;
        NextOffset = nextOffset;
        Diagnostics = diagnostics;
        Diagnostic = diagnostic;
    }
}
