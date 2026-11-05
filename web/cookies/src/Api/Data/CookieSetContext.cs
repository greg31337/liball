namespace cookies;

/// <summary>
/// Describes the origin and policy facts for accepting a cookie.
/// </summary>
public class CookieSetContext
{
    /// <summary>
    /// The absolute origin URI supplying the cookie.
    /// </summary>
    public global::System.Uri? Origin;

    /// <summary>
    /// The effective jar options used for cookie acceptance.
    /// </summary>
    public CookieJarOptions Options = new CookieJarOptions();

    /// <summary>
    /// The site relationship of the cookie-setting operation.
    /// </summary>
    public CookieSameSiteContext SameSiteContext;

    /// <summary>
    /// The API access kind of the cookie-setting operation.
    /// </summary>
    public CookieAccessKind AccessKind;

    /// <summary>
    /// The existing exact-identity cookie, when one is available for replacement checks.
    /// </summary>
    public StoredCookie? ExistingCookie;

    /// <summary>
    /// Existing domain candidates used for Secure-cookie overlay protection.
    /// </summary>
    public StoredCookie[] ExistingCookies = [];

    /// <summary>
    /// Indicates whether the cookie was supplied during a top-level navigation.
    /// </summary>
    public bool IsTopLevelNavigation;

    /// <summary>
    /// Indicates whether a top-level schemeful site is available.
    /// </summary>
    public bool HasTopLevelSite;

    /// <summary>
    /// The canonical top-level schemeful site when <see cref="HasTopLevelSite"/> is true.
    /// </summary>
    public string TopLevelSite = "";

    /// <summary>
    /// Indicates whether the operation is part of a snapshot import.
    /// </summary>
    public bool HasImportMode;

    /// <summary>
    /// The snapshot import behavior when <see cref="HasImportMode"/> is true.
    /// </summary>
    public CookieImportMode ImportMode;
}
