namespace whois;

/// <summary>
/// Defines destination policy evaluation for resolved WHOIS endpoints.
/// </summary>
public interface IWhoisEndpointPolicy
{
    /// <summary>
    /// Decides whether a resolved address is eligible for the endpoint source.
    /// </summary>
    /// <param name="endpoint">The advertised endpoint metadata.</param>
    /// <param name="address">The resolved literal address.</param>
    /// <param name="source">The source of endpoint selection.</param>
    /// <returns><c>true</c> when the address is eligible; otherwise <c>false</c>.</returns>
    bool AllowEndpoint(WhoisEndpoint endpoint, System.Net.IPAddress address, WhoisEndpointSource source);
}
