namespace cookies;

/// <summary>
/// Configures lossy Netscape cookie-file import and export behavior.
/// </summary>
/// <remarks>
/// Codec construction rejects negative limits and configurations that permit ambiguous partitioned state.
/// </remarks>
public class NetscapeCookieFileOptions
{
    /// <summary>
    /// The limits applied while importing cookie-file text.
    /// </summary>
    public CookieLimits Limits = new CookieLimits();

    /// <summary>
    /// Includes session cookies in exported text.
    /// </summary>
    public bool IncludeSessionCookies = true;

    /// <summary>
    /// Includes expired cookies in exported text.
    /// </summary>
    public bool IncludeExpiredCookies;

    /// <summary>
    /// Accepts session-cookie entries during import.
    /// </summary>
    public bool ImportSessionCookies = true;

    /// <summary>
    /// Accepts already-expired entries during import.
    /// </summary>
    public bool ImportExpiredCookies;

    /// <summary>
    /// Rejects partitioned cookies that cannot be represented without ambiguity.
    /// </summary>
    public bool RejectPartitionedCookies = true;
}
