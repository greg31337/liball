namespace fix;

internal class LimitCheckResult
{
    internal bool Allowed;
    internal int Actual;
    internal int Maximum;
    internal string ErrorCode = "";
}
