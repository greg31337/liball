namespace cookies;

/// <summary>
/// Provides portable default limits and timing values for cookie processing.
/// </summary>
public static class CookieDefaults
{
    /// <summary>
    /// The default maximum combined cookie name and value size in octets.
    /// </summary>
    public const int MaximumNameValueOctets = 4096;

    /// <summary>
    /// The default maximum attribute value size in octets.
    /// </summary>
    public const int MaximumAttributeValueOctets = 1024;

    /// <summary>
    /// The default maximum input line size in octets.
    /// </summary>
    public const int MaximumInputLineOctets = 65536;

    /// <summary>
    /// The default maximum number of cookies retained for one domain.
    /// </summary>
    public const int MaximumCookiesPerDomain = 50;

    /// <summary>
    /// The default maximum number of cookies retained by a store.
    /// </summary>
    public const int MaximumCookies = 3000;

    /// <summary>
    /// The default request Cookie field limit, where zero disables splitting by size.
    /// </summary>
    public const int MaximumRequestHeaderOctets = 0;

    /// <summary>
    /// The current lossless snapshot format version.
    /// </summary>
    public const int SnapshotVersion = 1;

    /// <summary>
    /// The default maximum persistent lifetime used by the modern profile.
    /// </summary>
    public static readonly global::System.TimeSpan MaximumLifetime = global::System.TimeSpan.FromDays(400);

    /// <summary>
    /// The default age window for optional Lax-allowing-unsafe enforcement.
    /// </summary>
    public static readonly global::System.TimeSpan LaxAllowingUnsafeGracePeriod = global::System.TimeSpan.FromMinutes(2);
}
