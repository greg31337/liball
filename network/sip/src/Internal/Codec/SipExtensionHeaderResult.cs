namespace sip;

internal class SipExtensionHeaderResult
{
    internal bool Success;
    internal SipExtensionHeaderValue[] Values = new SipExtensionHeaderValue[0];
    internal int ErrorOffset = -1;
    internal string? ErrorMessage;
}
