namespace sip;

internal class SipProxyOperationContext
{
    internal string? Id;
    internal SipProxyRequest? Request;
    internal SipProxyMode Mode;
    internal System.Collections.Generic.List<SipProxyTarget> Targets =
        new System.Collections.Generic.List<SipProxyTarget>();
    internal int NextTargetIndex;
    internal int CreatedBranchCount;
    internal bool CancelRequested;
}
