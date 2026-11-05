namespace sip;

internal class SipEventPackageEntry
{
    internal string EventPackage = "";
    internal ISipEventPackage? Handler;
    internal SipMediaType[] ContentTypes = new SipMediaType[0];
    internal bool BuiltIn;
    internal bool SupportsResourceLists;
}
