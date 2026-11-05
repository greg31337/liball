namespace cookies;

/// <summary>
/// Describes the site relationship of a cookie operation.
/// </summary>
public enum CookieSameSiteContext
{
    /// <summary>
    /// The operation occurs in a same-site context.
    /// </summary>
    SameSite = 0,

    /// <summary>
    /// The operation occurs in a cross-site context.
    /// </summary>
    CrossSite = 1
}
