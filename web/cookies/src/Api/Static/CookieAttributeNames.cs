namespace cookies;

/// <summary>
/// Provides canonical Set-Cookie attribute names and cookie name prefixes.
/// </summary>
public static class CookieAttributeNames
{
    /// <summary>
    /// The canonical Expires attribute name.
    /// </summary>
    public const string Expires = "Expires";

    /// <summary>
    /// The canonical Max-Age attribute name.
    /// </summary>
    public const string MaxAge = "Max-Age";

    /// <summary>
    /// The canonical Domain attribute name.
    /// </summary>
    public const string Domain = "Domain";

    /// <summary>
    /// The canonical Path attribute name.
    /// </summary>
    public const string Path = "Path";

    /// <summary>
    /// The canonical Secure attribute name.
    /// </summary>
    public const string Secure = "Secure";

    /// <summary>
    /// The canonical HttpOnly attribute name.
    /// </summary>
    public const string HttpOnly = "HttpOnly";

    /// <summary>
    /// The canonical SameSite attribute name.
    /// </summary>
    public const string SameSite = "SameSite";

    /// <summary>
    /// The canonical Partitioned attribute name.
    /// </summary>
    public const string Partitioned = "Partitioned";

    /// <summary>
    /// The canonical Secure cookie name prefix.
    /// </summary>
    public const string SecurePrefix = "__Secure-";

    /// <summary>
    /// The canonical Host cookie name prefix.
    /// </summary>
    public const string HostPrefix = "__Host-";
}
