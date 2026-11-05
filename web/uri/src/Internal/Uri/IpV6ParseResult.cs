namespace uri;

internal class IpV6ParseResult
{
    internal bool Success;
    internal byte[] AddressBytes;
    internal UriDiagnostic? Diagnostic;

    internal IpV6ParseResult(
        bool success,
        byte[] addressBytes,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        AddressBytes = addressBytes;
        Diagnostic = diagnostic;
    }
}
