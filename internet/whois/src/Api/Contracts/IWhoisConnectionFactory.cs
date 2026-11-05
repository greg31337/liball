namespace whois;

/// <summary>
/// Defines connection establishment to a previously approved IP address.
/// </summary>
public interface IWhoisConnectionFactory
{
    /// <summary>
    /// Opens a connection to an approved literal address without resolving the advertised host again.
    /// </summary>
    /// <param name="endpoint">The advertised endpoint metadata.</param>
    /// <param name="approvedAddress">The policy-approved literal address.</param>
    /// <param name="timeouts">The configured exchange timeouts.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The status-bearing connection result.</returns>
    WhoisConnection OpenConnection(WhoisEndpoint endpoint, System.Net.IPAddress approvedAddress, WhoisTimeoutOptions timeouts, System.Threading.CancellationToken cancellationToken);
}
