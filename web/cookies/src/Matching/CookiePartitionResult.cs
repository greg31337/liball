namespace cookies;

internal class CookiePartitionResult
{
    internal bool Success;
    internal bool Allowed;
    internal CookieErrorCode ErrorCode;
    internal bool EnforcementEnabled;
    internal bool AttributeObserved;
    internal bool AttributeIgnored;
    internal bool HasPartitionKey;
    internal string PartitionKey = "";
    internal bool PartitionMatched;
    internal CookieIssue[] Issues = [];
}
