namespace whois;

/// <summary>
/// Formats normalized WHOIS queries for IANA, ordinary, ARIN, and Verisign servers.
/// </summary>
public class WhoisQueryFormatter : IWhoisQueryFormatter
{
    /// <summary>
    /// Initializes the built-in WHOIS query formatter.
    /// </summary>
    public WhoisQueryFormatter()
    {
    }

    /// <summary>
    /// Formats a classified query for a selected server before CRLF framing.
    /// </summary>
    /// <param name="query">The classified and normalized query.</param>
    /// <param name="selection">The selected server and optional query override.</param>
    /// <returns>The exact server-specific query text before CRLF.</returns>
    public string FormatQuery(WhoisQuery query, WhoisServerSelection selection)
    {
        InputGuard.RequireReference(query, "query");
        InputGuard.RequireReference(selection, "selection");
        InputGuard.RequireReference(selection.Endpoint, "selection.Endpoint");
        InputGuard.RequireReference(selection.Endpoint.Host, "selection.Endpoint.Host");
        InputGuard.RequireReference(query.NormalizedText, "query.NormalizedText");

        if (selection.QueryOverride != null)
        {
            return selection.QueryOverride;
        }

        string host = NormalizeHost(selection.Endpoint.Host);
        if (host == "whois.arin.net")
        {
            if (query.Kind == WhoisQueryKind.IPv4Address || query.Kind == WhoisQueryKind.IPv6Address || query.Kind == WhoisQueryKind.IPv4Prefix || query.Kind == WhoisQueryKind.IPv6Prefix)
            {
                return "n + " + query.NormalizedText;
            }
            if (query.Kind == WhoisQueryKind.AutonomousSystem)
            {
                return "a + " + query.NormalizedText;
            }
        }

        if (host == "whois.verisign-grs.com" && query.Kind == WhoisQueryKind.Domain)
        {
            return "domain " + query.NormalizedText;
        }

        return query.NormalizedText;
    }

    internal static string NormalizeHost(string host)
    {
        string normalized = host.Trim().ToLowerInvariant();
        while (normalized.Length > 0 && normalized[normalized.Length - 1] == '.')
        {
            normalized = normalized.Substring(0, normalized.Length - 1);
        }
        return normalized;
    }
}
