namespace uri;

internal class UriQueryParseCoreResult
{
    internal bool Success;
    internal UriQueryParameterValue[] Parameters;
    internal char PairDelimiter;
    internal UriDiagnostic? Diagnostic;

    internal UriQueryParseCoreResult(
        bool success,
        UriQueryParameterValue[] parameters,
        char pairDelimiter,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Parameters = parameters;
        PairDelimiter = pairDelimiter;
        Diagnostic = diagnostic;
    }
}
