namespace whois;

/// <summary>
/// Defines one status-bearing RFC 3912 wire exchange.
/// </summary>
public interface IWhoisTransport
{
    /// <summary>
    /// Sends one exact query and reads response bytes until EOF or failure.
    /// </summary>
    /// <param name="request">The exact exchange request.</param>
    /// <returns>The raw response and exchange status.</returns>
    WhoisRawResponse Exchange(WhoisRequest request);
}
