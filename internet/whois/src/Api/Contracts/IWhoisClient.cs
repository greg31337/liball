namespace whois;

/// <summary>
/// Defines direct exchanges and automatic WHOIS lookup behavior.
/// </summary>
public interface IWhoisClient
{
    /// <summary>
    /// Performs exactly one caller-directed exchange and throws on operational failure.
    /// </summary>
    /// <param name="request">The exact caller-directed exchange request.</param>
    /// <returns>The complete raw response.</returns>
    WhoisRawResponse Query(WhoisRequest request);

    /// <summary>
    /// Performs exactly one caller-directed status-bearing exchange.
    /// </summary>
    /// <param name="request">The exact caller-directed exchange request.</param>
    /// <returns>The raw response and expected failure status.</returns>
    WhoisRawResponse TryQuery(WhoisRequest request);

    /// <summary>
    /// Performs an automatic WHOIS lookup and throws on operational failure.
    /// </summary>
    /// <param name="request">The caller input and lookup options.</param>
    /// <returns>The complete ordered lookup result.</returns>
    WhoisLookupResult Lookup(WhoisLookupRequest request);

    /// <summary>
    /// Performs an exception-less automatic WHOIS lookup.
    /// </summary>
    /// <param name="request">The caller input and lookup options.</param>
    /// <returns>The status-bearing lookup result.</returns>
    WhoisLookupResult TryLookup(WhoisLookupRequest request);
}
