namespace whois;

/// <summary>
/// Allows public-unicast destinations while restricting automatic endpoints to WHOIS service ports.
/// </summary>
public class WhoisPublicEndpointPolicy : IWhoisEndpointPolicy
{
    /// <summary>
    /// Initializes the safe public endpoint policy.
    /// </summary>
    public WhoisPublicEndpointPolicy()
    {
    }

    /// <summary>
    /// Decides whether a resolved public address and endpoint source are eligible.
    /// </summary>
    /// <param name="endpoint">The advertised endpoint metadata.</param>
    /// <param name="address">The resolved literal address.</param>
    /// <param name="source">The source of endpoint selection.</param>
    /// <returns><c>true</c> for an eligible public-unicast destination; otherwise <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">The endpoint or address is <c>null</c>.</exception>
    public bool AllowEndpoint(WhoisEndpoint endpoint, System.Net.IPAddress address, WhoisEndpointSource source)
    {
        InputGuard.RequireReference(endpoint, "endpoint");
        InputGuard.RequireReference(address, "address");

        if (!AddressClassifier.IsPublicUnicast(address))
        {
            return false;
        }
        if (endpoint.Port < 1 || endpoint.Port > 65535)
        {
            return false;
        }
        if (source == WhoisEndpointSource.Direct)
        {
            return true;
        }
        if (source == WhoisEndpointSource.InitialSelection || source == WhoisEndpointSource.Referral)
        {
            return endpoint.Port == WhoisDefaults.WhoisPort || endpoint.Port == WhoisDefaults.RWhoisPort;
        }
        return false;
    }
}
