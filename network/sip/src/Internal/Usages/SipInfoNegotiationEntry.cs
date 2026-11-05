namespace sip;

internal class SipInfoNegotiationEntry
{
    internal SipDialogId DialogId = new SipDialogId();
    internal string[] RemotePackages = new string[0];
    internal bool RemoteSupportsInfoPackages;
    internal string? CorrelationId;
}
