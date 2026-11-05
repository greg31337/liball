namespace uri;

internal class WebLinkFieldParseResult
{
    internal bool Success;
    internal WebLink[] Links;
    internal UriDiagnostic[] Diagnostics;
    internal UriDiagnostic? Diagnostic;

    internal WebLinkFieldParseResult(
        bool success,
        WebLink[] links,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Links = links;
        Diagnostics = new UriDiagnostic[0];
        Diagnostic = diagnostic;
    }
}
