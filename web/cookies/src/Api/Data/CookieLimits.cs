namespace cookies;

/// <summary>
/// Defines size, storage, header, and lifetime limits for cookie processing.
/// </summary>
/// <remarks>
/// Negative values are invalid. A zero request-header limit disables splitting by size.
/// </remarks>
public class CookieLimits
{
    /// <summary>
    /// The maximum combined cookie name and value size in octets.
    /// </summary>
    public int MaximumNameValueOctets = CookieDefaults.MaximumNameValueOctets;

    /// <summary>
    /// The maximum attribute value size in octets.
    /// </summary>
    public int MaximumAttributeValueOctets = CookieDefaults.MaximumAttributeValueOctets;

    /// <summary>
    /// The maximum input line size in octets.
    /// </summary>
    public int MaximumInputLineOctets = CookieDefaults.MaximumInputLineOctets;

    /// <summary>
    /// The maximum number of cookies retained for one domain.
    /// </summary>
    public int MaximumCookiesPerDomain = CookieDefaults.MaximumCookiesPerDomain;

    /// <summary>
    /// The maximum number of cookies retained by a store.
    /// </summary>
    public int MaximumCookies = CookieDefaults.MaximumCookies;

    /// <summary>
    /// The maximum request Cookie field size in octets, or zero for no size split.
    /// </summary>
    public int MaximumRequestHeaderOctets = CookieDefaults.MaximumRequestHeaderOctets;

    /// <summary>
    /// The maximum persistent cookie lifetime.
    /// </summary>
    public global::System.TimeSpan MaximumLifetime = CookieDefaults.MaximumLifetime;
}
