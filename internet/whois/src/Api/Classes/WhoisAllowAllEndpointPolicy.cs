namespace whois;

/// <summary>
/// Allows resolved destinations for explicitly trusted endpoint use.
/// </summary>
public class WhoisAllowAllEndpointPolicy : IWhoisEndpointPolicy
{
    /// <summary>
    /// Initializes the endpoint policy that allows trusted destinations.
    /// </summary>
    public WhoisAllowAllEndpointPolicy()
    {
    }

    /// <summary>
    /// Allows a resolved destination without address, port, or source restrictions.
    /// </summary>
    /// <param name="endpoint">The advertised endpoint metadata.</param>
    /// <param name="address">The resolved literal address.</param>
    /// <param name="source">The source of endpoint selection.</param>
    /// <returns>Always <c>true</c> for non-null endpoint and address inputs.</returns>
    /// <exception cref="System.ArgumentNullException">The endpoint or address is <c>null</c>.</exception>
    public bool AllowEndpoint(WhoisEndpoint endpoint, System.Net.IPAddress address, WhoisEndpointSource source)
    {
        InputGuard.RequireReference(endpoint, "endpoint");
        InputGuard.RequireReference(address, "address");
        return true;
    }
}
