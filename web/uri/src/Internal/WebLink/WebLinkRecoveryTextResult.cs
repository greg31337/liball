namespace uri;

internal class WebLinkRecoveryTextResult
{
    internal string Value;
    internal int NextOffset;
    internal UriDiagnostic[] Diagnostics;

    internal WebLinkRecoveryTextResult(
        string value,
        int nextOffset,
        UriDiagnostic[] diagnostics)
    {
        Value = value;
        NextOffset = nextOffset;
        Diagnostics = diagnostics;
    }
}
