namespace uri;

internal class IpV4ParseResult
{
    internal bool Success;
    internal byte[] AddressBytes;
    internal UriDiagnostic? Diagnostic;

    internal IpV4ParseResult(
        bool success,
        byte[] addressBytes,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        AddressBytes = addressBytes;
        Diagnostic = diagnostic;
    }
}
