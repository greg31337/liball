namespace uri;

internal class IpFutureParseResult
{
    internal bool Success;
    internal int VersionOffset;
    internal int VersionLength;
    internal int AddressOffset;
    internal int AddressLength;
    internal UriDiagnostic? Diagnostic;

    internal IpFutureParseResult(
        bool success,
        int versionOffset,
        int versionLength,
        int addressOffset,
        int addressLength,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        VersionOffset = versionOffset;
        VersionLength = versionLength;
        AddressOffset = addressOffset;
        AddressLength = addressLength;
        Diagnostic = diagnostic;
    }
}
