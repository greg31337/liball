namespace whois;

/// <summary>
/// Provides well-known field and directive names that advertise referrals.
/// </summary>
public static class WhoisFieldNames
{
    /// <summary>
    /// The IANA referral field name.
    /// </summary>
    public const string Refer = "refer";

    /// <summary>
    /// The generic WHOIS service field name.
    /// </summary>
    public const string Whois = "whois";

    /// <summary>
    /// The legacy WHOIS server field name.
    /// </summary>
    public const string WhoisServer = "Whois Server";

    /// <summary>
    /// The registrar WHOIS server field name.
    /// </summary>
    public const string RegistrarWhoisServer = "Registrar WHOIS Server";

    /// <summary>
    /// The compact referral server field name.
    /// </summary>
    public const string ReferralServer = "ReferralServer";

    /// <summary>
    /// The spaced referral server field name.
    /// </summary>
    public const string ReferralServerWithSpace = "Referral Server";

    /// <summary>
    /// The RWhois referral directive name.
    /// </summary>
    public const string ReferralDirective = "%referral";
}
