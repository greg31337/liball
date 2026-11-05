namespace whois;

/// <summary>
/// Describes the protocol, host, and port of a WHOIS-related endpoint.
/// </summary>
public class WhoisEndpoint
{
    /// <summary>
    /// Identifies the endpoint protocol.
    /// </summary>
    public WhoisReferralScheme Scheme = WhoisReferralScheme.Whois;

    /// <summary>
    /// Contains the endpoint host name or IP literal.
    /// </summary>
    public string Host = string.Empty;

    /// <summary>
    /// Contains the endpoint TCP port.
    /// </summary>
    public int Port = WhoisDefaults.WhoisPort;

    /// <summary>
    /// Contains the endpoint value as originally advertised.
    /// </summary>
    public string AdvertisedValue = string.Empty;
}
