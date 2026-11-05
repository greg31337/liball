namespace cookies;

internal class CookieCanonicalHostResult
{
    internal bool Success;
    internal CookieErrorCode ErrorCode;
    internal string Host = "";
    internal bool IsDnsName;
    internal bool IsIpv4;
    internal bool IsIpv6;
}
