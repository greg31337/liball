using whois;

namespace whois.Tests;

internal class FakeHostResolver : IWhoisHostResolver
{
    public WhoisResolution Result = new WhoisResolution();
    public Func<WhoisEndpoint, WhoisAddressPreference, CancellationToken, WhoisResolution>? ResolveHandler = null;
    public List<WhoisEndpoint> Endpoints = new List<WhoisEndpoint>();
    public List<WhoisAddressPreference> Preferences = new List<WhoisAddressPreference>();
    public List<CancellationToken> CancellationTokens = new List<CancellationToken>();

    public WhoisResolution ResolveHost(WhoisEndpoint endpoint, WhoisAddressPreference preference, CancellationToken cancellationToken)
    {
        Endpoints.Add(endpoint);
        Preferences.Add(preference);
        CancellationTokens.Add(cancellationToken);
        if (ResolveHandler != null)
        {
            return ResolveHandler(endpoint, preference, cancellationToken);
        }
        return Result;
    }
}
