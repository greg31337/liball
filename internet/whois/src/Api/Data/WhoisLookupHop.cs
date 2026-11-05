namespace whois;

/// <summary>
/// Contains complete provenance for one server exchange in an automatic lookup.
/// </summary>
public class WhoisLookupHop
{
    /// <summary>
    /// Contains the endpoint selection metadata for this hop.
    /// </summary>
    public WhoisServerSelection Selection = new WhoisServerSelection();

    /// <summary>
    /// Contains the raw response and exchange status for this hop.
    /// </summary>
    public WhoisRawResponse RawResponse = new WhoisRawResponse();

    /// <summary>
    /// Contains the decoded response view for this hop.
    /// </summary>
    public WhoisTextResponse TextResponse = new WhoisTextResponse();

    /// <summary>
    /// Contains the ordered generic parsed response for this hop.
    /// </summary>
    public WhoisParsedResponse ParsedResponse = new WhoisParsedResponse();

    /// <summary>
    /// Contains every recognized or rejected referral in response order.
    /// </summary>
    public WhoisReferral[] Referrals = System.Array.Empty<WhoisReferral>();

    /// <summary>
    /// Contains the referral selected for the next hop, or <c>null</c> when traversal stops.
    /// </summary>
    public WhoisReferral? SelectedReferral = null;
}
