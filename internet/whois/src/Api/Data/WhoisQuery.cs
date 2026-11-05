namespace whois;

/// <summary>
/// Contains the original and normalized forms of a classified WHOIS query.
/// </summary>
public class WhoisQuery
{
    /// <summary>
    /// Contains the query text supplied by the caller.
    /// </summary>
    public string OriginalText = string.Empty;

    /// <summary>
    /// Contains the normalized query text sent to an ordinary server.
    /// </summary>
    public string NormalizedText = string.Empty;

    /// <summary>
    /// Contains the stable normalized key used for routing and loop detection.
    /// </summary>
    public string RoutingKey = string.Empty;

    /// <summary>
    /// Identifies the classified resource kind.
    /// </summary>
    public WhoisQueryKind Kind = WhoisQueryKind.Automatic;

    /// <summary>
    /// Indicates whether classification and normalization succeeded.
    /// </summary>
    public bool IsValid = false;

    /// <summary>
    /// Contains validation context when <see cref="IsValid"/> is <c>false</c>.
    /// </summary>
    public string ValidationMessage = string.Empty;
}
