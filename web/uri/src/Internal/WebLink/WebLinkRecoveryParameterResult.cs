namespace uri;

internal class WebLinkRecoveryParameterResult
{
    internal bool Success;
    internal WebLinkParameter Parameter;
    internal int NextOffset;
    internal UriDiagnostic[] Diagnostics;
    internal UriDiagnostic? Diagnostic;

    internal WebLinkRecoveryParameterResult(
        bool success,
        WebLinkParameter parameter,
        int nextOffset,
        UriDiagnostic[] diagnostics,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Parameter = parameter;
        NextOffset = nextOffset;
        Diagnostics = diagnostics;
        Diagnostic = diagnostic;
    }
}
