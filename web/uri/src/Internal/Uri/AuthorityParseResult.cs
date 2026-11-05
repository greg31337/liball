namespace uri;

internal class AuthorityParseResult
{
    internal bool Success;
    internal bool HasUserInfo;
    internal int UserInfoOffset;
    internal int UserInfoLength;
    internal bool HostBracketed;
    internal int HostOffset;
    internal int HostLength;
    internal bool HasPort;
    internal int PortOffset;
    internal int PortLength;
    internal UriDiagnostic? Diagnostic;

    internal AuthorityParseResult(
        bool success,
        bool hasUserInfo,
        int userInfoOffset,
        int userInfoLength,
        bool hostBracketed,
        int hostOffset,
        int hostLength,
        bool hasPort,
        int portOffset,
        int portLength,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        HasUserInfo = hasUserInfo;
        UserInfoOffset = userInfoOffset;
        UserInfoLength = userInfoLength;
        HostBracketed = hostBracketed;
        HostOffset = hostOffset;
        HostLength = hostLength;
        HasPort = hasPort;
        PortOffset = portOffset;
        PortLength = portLength;
        Diagnostic = diagnostic;
    }
}
