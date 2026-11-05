namespace cookies;

/// <summary>
/// Configures cookie-jar compliance, policy, interoperability, and eviction behavior.
/// </summary>
/// <remarks>
/// <para>Negative grace periods and limits are invalid.</para>
/// <para>Nested parser and serializer profiles and limits must not contradict the jar settings.</para>
/// <para>Disabling public-suffix protection is an explicit insecure or testing configuration.</para>
/// </remarks>
public class CookieJarOptions
{
    /// <summary>
    /// The standards profile used by the jar.
    /// </summary>
    public CookieComplianceMode ComplianceMode = CookieComplianceMode.Rfc6265;

    /// <summary>
    /// The limits enforced by storage and selection operations.
    /// </summary>
    public CookieLimits Limits = new CookieLimits();

    /// <summary>
    /// The parsing defaults used for raw cookie fields.
    /// </summary>
    public CookieParseOptions ParseOptions = new CookieParseOptions();

    /// <summary>
    /// The serialization defaults used for request cookie fields.
    /// </summary>
    public CookieSerializationOptions SerializationOptions = new CookieSerializationOptions();

    /// <summary>
    /// The default Netscape cookie-file interoperability choices.
    /// </summary>
    public NetscapeCookieFileOptions NetscapeFileOptions = new NetscapeCookieFileOptions();

    /// <summary>
    /// Enables SameSite creation and retrieval enforcement.
    /// </summary>
    public bool EnforceSameSite;

    /// <summary>
    /// The SameSite mode applied to cookies whose parsed mode is Default.
    /// </summary>
    public CookieSameSiteMode DefaultSameSiteMode = CookieSameSiteMode.Default;

    /// <summary>
    /// Enables the optional short-lived Lax-allowing-unsafe behavior.
    /// </summary>
    public bool EnableLaxAllowingUnsafe;

    /// <summary>
    /// The maximum cookie age eligible for Lax-allowing-unsafe behavior.
    /// </summary>
    public global::System.TimeSpan LaxAllowingUnsafeGracePeriod = CookieDefaults.LaxAllowingUnsafeGracePeriod;

    /// <summary>
    /// Requires Secure on cookies with an explicit SameSite None mode.
    /// </summary>
    public bool RequireSecureForSameSiteNone;

    /// <summary>
    /// The cookie name prefix enforcement policy.
    /// </summary>
    public CookiePrefixPolicy PrefixPolicy = CookiePrefixPolicy.Disabled;

    /// <summary>
    /// Uses case-insensitive cookie name prefix matching.
    /// </summary>
    public bool UseCaseInsensitivePrefixes;

    /// <summary>
    /// Requires a public-suffix provider for domain-cookie protection.
    /// </summary>
    public bool RequirePublicSuffixList = true;

    /// <summary>
    /// Enables Partitioned cookie parsing, storage, and matching.
    /// </summary>
    public bool EnablePartitionedCookies;

    /// <summary>
    /// Canonical domains and their subdomains allowed by application policy, or empty for all.
    /// </summary>
    public string[] AllowedDomains = [];

    /// <summary>
    /// Canonical domains and their subdomains blocked by application policy.
    /// </summary>
    public string[] BlockedDomains = [];

    /// <summary>
    /// Treats loopback and localhost origins as potentially trustworthy.
    /// </summary>
    public bool TreatLoopbackAsSecure;

    /// <summary>
    /// Prevents cookies received over insecure origins from overlaying Secure cookies.
    /// </summary>
    public bool ProtectSecureCookieOverlay;

    /// <summary>
    /// Prefers non-Secure cookies when selecting quota-eviction candidates.
    /// </summary>
    public bool EvictNonSecureCookiesFirst = true;
}
