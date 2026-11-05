namespace sip;

internal class SipTransferHeaderParse
{
    internal SipNameAddress? ReferTo;
    internal SipNameAddress? ReferredBy;
    internal SipExtensionHeaderValue? Replaces;
    internal SipExtensionHeaderValue? Join;
    internal SipExtensionHeaderValue? TargetDialog;
    internal int StatusCode;
    internal string? ErrorMessage;
}
