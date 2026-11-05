namespace whois;

/// <summary>
/// Identifies how a WHOIS endpoint was selected.
/// </summary>
public enum WhoisEndpointSource
{
    /// <summary>
    /// The caller supplied the endpoint for a direct exchange.
    /// </summary>
    Direct = 0,

    /// <summary>
    /// A server selector supplied the initial endpoint for a lookup.
    /// </summary>
    InitialSelection = 1,

    /// <summary>
    /// A prior WHOIS response advertised the endpoint as a referral.
    /// </summary>
    Referral = 2
}
