namespace sip;

internal class SipInfoPackageEntry
{
    internal string Package = "";
    internal ISipInfoPackage? Handler;
    internal SipMediaType[] ContentTypes = new SipMediaType[0];
}
