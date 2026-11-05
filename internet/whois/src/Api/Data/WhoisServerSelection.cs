namespace whois;

/// <summary>
/// Describes the provenance of a selected WHOIS server.
/// </summary>
public class WhoisServerSelection
{
    /// <summary>
    /// Contains the selected endpoint.
    /// </summary>
    public WhoisEndpoint Endpoint = new WhoisEndpoint();

    /// <summary>
    /// Identifies how the server was selected.
    /// </summary>
    public WhoisEndpointSource Source = WhoisEndpointSource.InitialSelection;

    /// <summary>
    /// Contains a server-specific query override, or <c>null</c> to use normal formatting.
    /// </summary>
    public string? QueryOverride = null;

    /// <summary>
    /// Contains diagnostic context describing the server selection.
    /// </summary>
    public string Reason = string.Empty;
}
