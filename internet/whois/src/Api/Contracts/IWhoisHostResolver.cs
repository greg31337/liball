namespace whois;

/// <summary>
/// Defines deterministic host resolution for WHOIS endpoints.
/// </summary>
public interface IWhoisHostResolver
{
    /// <summary>
    /// Resolves an endpoint host into ordered IP address candidates.
    /// </summary>
    /// <param name="endpoint">The endpoint whose host is resolved.</param>
    /// <param name="preference">The preferred address-family ordering.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The status-bearing resolution result.</returns>
    WhoisResolution ResolveHost(WhoisEndpoint endpoint, WhoisAddressPreference preference, System.Threading.CancellationToken cancellationToken);
}
