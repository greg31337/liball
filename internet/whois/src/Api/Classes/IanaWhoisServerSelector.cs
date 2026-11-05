namespace whois;

/// <summary>
/// Selects the IANA WHOIS bootstrap server for automatically routable resource queries.
/// </summary>
public class IanaWhoisServerSelector : IWhoisServerSelector
{
    /// <summary>
    /// Initializes the built-in IANA WHOIS server selector.
    /// </summary>
    public IanaWhoisServerSelector()
    {
    }

    /// <summary>
    /// Selects a normalized caller override or the IANA WHOIS bootstrap endpoint.
    /// </summary>
    /// <param name="query">The classified and normalized query.</param>
    /// <param name="request">The original lookup request and endpoint override.</param>
    /// <returns>The selected endpoint and its provenance.</returns>
    /// <exception cref="System.ArgumentNullException">The query or request is <c>null</c>.</exception>
    /// <exception cref="WhoisQueryException">The caller endpoint override is invalid.</exception>
    /// <exception cref="WhoisServerNotFoundException">The query kind cannot be routed automatically.</exception>
    public WhoisServerSelection SelectServer(WhoisQuery query, WhoisLookupRequest request)
    {
        InputGuard.RequireReference(query, "query");
        InputGuard.RequireReference(request, "request");
        if (request.EndpointOverride != null)
        {
            return CreateOverrideSelection(query, request.EndpointOverride);
        }
        if (!query.IsValid || !IsAutomaticallyRoutable(query.Kind))
        {
            throw new WhoisServerNotFoundException("No automatic WHOIS server is available for the classified query.", query, null);
        }
        return CreateIanaSelection();
    }

    internal static WhoisServerSelection CreateOverrideSelection(WhoisQuery query, WhoisEndpoint endpointOverride)
    {
        InputGuard.ValidateEndpoint(endpointOverride, query.OriginalText, query.Kind);
        if (endpointOverride.Scheme != WhoisReferralScheme.Whois && endpointOverride.Scheme != WhoisReferralScheme.Unspecified)
        {
            throw InputGuard.CreateQueryError("The endpoint override must use the WHOIS protocol.", "Endpoint.Scheme", query.OriginalText, query.Kind, endpointOverride);
        }

        string host = endpointOverride.Host;
        if (System.Uri.CheckHostName(host) == System.UriHostNameType.IPv6 && host[0] != '[')
        {
            host = "[" + host + "]";
        }
        string authority = host + ":" + endpointOverride.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        EndpointParseResult parsed = EndpointParser.ParseValue(authority);
        WhoisEndpoint? normalizedEndpoint = parsed.Endpoint;
        if (!parsed.IsValid || normalizedEndpoint == null)
        {
            throw InputGuard.CreateQueryError(parsed.RejectionReason, "Endpoint", query.OriginalText, query.Kind, endpointOverride);
        }
        normalizedEndpoint.AdvertisedValue = endpointOverride.AdvertisedValue;

        WhoisServerSelection selection = new WhoisServerSelection();
        selection.Endpoint = normalizedEndpoint;
        selection.Source = WhoisEndpointSource.Direct;
        selection.QueryOverride = null;
        selection.Reason = "Selected the caller-provided endpoint override.";
        return selection;
    }

    internal static WhoisServerSelection CreateIanaSelection()
    {
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Scheme = WhoisReferralScheme.Whois;
        endpoint.Host = WhoisDefaults.IanaHost;
        endpoint.Port = WhoisDefaults.WhoisPort;
        endpoint.AdvertisedValue = WhoisDefaults.IanaHost;

        WhoisServerSelection selection = new WhoisServerSelection();
        selection.Endpoint = endpoint;
        selection.Source = WhoisEndpointSource.InitialSelection;
        selection.QueryOverride = null;
        selection.Reason = "Selected the IANA WHOIS bootstrap server.";
        return selection;
    }

    internal static bool IsAutomaticallyRoutable(WhoisQueryKind kind)
    {
        return kind == WhoisQueryKind.Domain ||
            kind == WhoisQueryKind.IPv4Address ||
            kind == WhoisQueryKind.IPv6Address ||
            kind == WhoisQueryKind.IPv4Prefix ||
            kind == WhoisQueryKind.IPv6Prefix ||
            kind == WhoisQueryKind.AutonomousSystem ||
            kind == WhoisQueryKind.ReverseDomain;
    }
}
