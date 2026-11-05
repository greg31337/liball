using whois;

namespace whois.Tests;

internal class FakeEndpointPolicy : IWhoisEndpointPolicy
{
    public bool Allowed = true;
    public Func<WhoisEndpoint, System.Net.IPAddress, WhoisEndpointSource, bool>? AllowHandler = null;
    public List<WhoisEndpoint> Endpoints = new List<WhoisEndpoint>();
    public List<System.Net.IPAddress> Addresses = new List<System.Net.IPAddress>();
    public List<WhoisEndpointSource> Sources = new List<WhoisEndpointSource>();

    public bool AllowEndpoint(WhoisEndpoint endpoint, System.Net.IPAddress address, WhoisEndpointSource source)
    {
        Endpoints.Add(endpoint);
        Addresses.Add(address);
        Sources.Add(source);
        if (AllowHandler != null)
        {
            return AllowHandler(endpoint, address, source);
        }
        return Allowed;
    }
}
