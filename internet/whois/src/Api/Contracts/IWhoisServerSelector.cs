namespace whois;

/// <summary>
/// Defines initial WHOIS server selection for a classified query.
/// </summary>
public interface IWhoisServerSelector
{
    /// <summary>
    /// Selects the initial endpoint or returns selection failure context.
    /// </summary>
    /// <param name="query">The classified and normalized query.</param>
    /// <param name="request">The original lookup request and overrides.</param>
    /// <returns>The selected endpoint and its provenance.</returns>
    WhoisServerSelection SelectServer(WhoisQuery query, WhoisLookupRequest request);
}
