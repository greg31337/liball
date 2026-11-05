namespace cookies;

internal class CookieDomainResult
{
    internal bool Success;
    internal CookieErrorCode ErrorCode;
    internal string Domain = "";
    internal bool HostOnly;
    internal bool DomainAttributeSupplied;
    internal bool IsIpAddress;
}
