namespace cookies;

/// <summary>
/// Specifies the SameSite behavior of a cookie.
/// </summary>
public enum CookieSameSiteMode
{
    /// <summary>
    /// Uses the profile default because SameSite was absent or unrecognized.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Allows same-site requests and eligible top-level cross-site navigations.
    /// </summary>
    Lax = 1,

    /// <summary>
    /// Allows only same-site requests.
    /// </summary>
    Strict = 2,

    /// <summary>
    /// Allows same-site and cross-site requests subject to other cookie rules.
    /// </summary>
    None = 3
}
