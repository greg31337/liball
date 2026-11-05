namespace sip;

internal class SipConnectionAlias
{
    internal SipReliableConnectionCore? Connection;
    internal SipEndpointAddress? Destination;
    internal string[] Identities = new string[0];
    internal bool RequireMutualTls;
}
