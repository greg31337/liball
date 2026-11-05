namespace uri;

internal class UriQueryParameterConversionResult
{
    internal bool Success;
    internal UriQueryParameterValue[] Parameters;
    internal UriDiagnostic? Diagnostic;

    internal UriQueryParameterConversionResult(
        bool success,
        UriQueryParameterValue[] parameters,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Parameters = parameters;
        Diagnostic = diagnostic;
    }
}
