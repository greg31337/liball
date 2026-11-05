namespace whois;

/// <summary>
/// Defines endpoint-specific WHOIS query formatting.
/// </summary>
public interface IWhoisQueryFormatter
{
    /// <summary>
    /// Formats a classified query for a selected server before CRLF framing.
    /// </summary>
    /// <param name="query">The classified and normalized query.</param>
    /// <param name="selection">The selected server and optional query override.</param>
    /// <returns>The exact server-specific query text before CRLF.</returns>
    string FormatQuery(WhoisQuery query, WhoisServerSelection selection);
}
