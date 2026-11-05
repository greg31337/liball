namespace uri;

internal class WebLinkParameterParseResult
{
    internal bool Success;
    internal WebLinkParameter Parameter;
    internal int NextOffset;
    internal UriDiagnostic? Diagnostic;

    internal WebLinkParameterParseResult(
        bool success,
        WebLinkParameter parameter,
        int nextOffset,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Parameter = parameter;
        NextOffset = nextOffset;
        Diagnostic = diagnostic;
    }
}
