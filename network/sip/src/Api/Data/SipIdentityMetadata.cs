namespace sip;

/// <summary>Holds identity, privacy, history, session, location, and priority metadata.</summary>
public class SipIdentityMetadata
{
    /// <summary>The ordered Privacy header tokens, including extension tokens.</summary>
    public string[] Privacy = new string[0];
    /// <summary>The asserted identities supplied by a trusted network.</summary>
    public SipExtensionHeaderValue[] AssertedIdentities =
        new SipExtensionHeaderValue[0];
    /// <summary>The preferred identities supplied to a trusted network.</summary>
    public SipExtensionHeaderValue[] PreferredIdentities =
        new SipExtensionHeaderValue[0];
    /// <summary>The identity presented by From on requests or To on responses.</summary>
    public SipNameAddress? ConnectedIdentity;
    /// <summary>The ordered History-Info values and their extension parameters.</summary>
    public SipExtensionHeaderValue[] HistoryInformation =
        new SipExtensionHeaderValue[0];
    /// <summary>The Session-ID value and its remote and extension parameters.</summary>
    public SipExtensionHeaderValue? SessionIdentifier;
    /// <summary>The ordered Geolocation references and their extension parameters.</summary>
    public SipExtensionHeaderValue[] Geolocations =
        new SipExtensionHeaderValue[0];
    /// <summary>The Geolocation-Routing field value, including extension tokens.</summary>
    public string? GeolocationRouting;
    /// <summary>The raw Geolocation-Error fields in wire order.</summary>
    public SipHeader[] GeolocationErrors = new SipHeader[0];
    /// <summary>The ordered Resource-Priority namespace and priority values.</summary>
    public SipExtensionHeaderValue[] ResourcePriorities =
        new SipExtensionHeaderValue[0];
    /// <summary>The ordered accepted Resource-Priority namespace and priority values.</summary>
    public SipExtensionHeaderValue[] AcceptedResourcePriorities =
        new SipExtensionHeaderValue[0];
    /// <summary>The raw Identity and legacy Identity-Info header fields in wire order.</summary>
    public SipHeader[] IdentityHeaders = new SipHeader[0];
    /// <summary>The full or compact PASSporT values found in Identity fields.</summary>
    public string[] Passports = new string[0];
}
