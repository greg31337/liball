namespace cookies;

internal class CookiePublicSuffixResult
{
    internal bool Success;
    internal CookieErrorCode ErrorCode;
    internal string Domain = "";
    internal bool HostOnly;
    internal bool DomainAttributeSupplied;
    internal bool IsIpAddress;
    internal CookieIssue[] Issues = [];
}
