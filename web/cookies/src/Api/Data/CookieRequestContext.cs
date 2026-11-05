namespace cookies;

/// <summary>
/// Describes the request and policy facts used to select cookies.
/// </summary>
public class CookieRequestContext
{
    /// <summary>
    /// The absolute request URI receiving selected cookies.
    /// </summary>
    public global::System.Uri? RequestUri;

    /// <summary>
    /// The request method used for SameSite safe-method classification.
    /// </summary>
    public string RequestMethod = "";

    /// <summary>
    /// The site relationship of the request.
    /// </summary>
    public CookieSameSiteContext SameSiteContext;

    /// <summary>
    /// The API access kind of the request.
    /// </summary>
    public CookieAccessKind AccessKind;

    /// <summary>
    /// Indicates whether the request is a top-level navigation.
    /// </summary>
    public bool IsTopLevelNavigation;

    /// <summary>
    /// Indicates whether the request is a reload navigation.
    /// </summary>
    public bool IsReload;

    /// <summary>
    /// Indicates whether the request redirect chain crossed a site boundary.
    /// </summary>
    public bool RedirectChainIsCrossSite;

    /// <summary>
    /// Indicates whether the request has user activation.
    /// </summary>
    public bool HasUserActivation;

    /// <summary>
    /// Indicates whether a top-level schemeful site is available.
    /// </summary>
    public bool HasTopLevelSite;

    /// <summary>
    /// The canonical top-level schemeful site when <see cref="HasTopLevelSite"/> is true.
    /// </summary>
    public string TopLevelSite = "";
}
