namespace uri;

internal class WebLinkValueParseResult
{
    internal bool Success;
    internal WebLink Link;
    internal int NextOffset;
    internal UriDiagnostic[] Diagnostics;
    internal UriDiagnostic? Diagnostic;

    internal WebLinkValueParseResult(
        bool success,
        WebLink link,
        int nextOffset,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Link = link;
        NextOffset = nextOffset;
        Diagnostics = new UriDiagnostic[0];
        Diagnostic = diagnostic;
    }
}
