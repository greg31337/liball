namespace cookies;

/// <summary>
/// Represents a parsed or producer-supplied Set-Cookie value.
/// </summary>
public class SetCookie
{
    /// <summary>
    /// The cookie name.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// The cookie value.
    /// </summary>
    public string Value = "";

    /// <summary>
    /// Indicates that the cookie value should retain compatible quoted form.
    /// </summary>
    public bool IsValueQuoted;

    /// <summary>
    /// Indicates whether a valid Expires attribute was supplied.
    /// </summary>
    public bool HasExpires;

    /// <summary>
    /// The Expires instant when <see cref="HasExpires"/> is true.
    /// </summary>
    public global::System.DateTimeOffset Expires;

    /// <summary>
    /// Indicates whether a valid Max-Age attribute was supplied.
    /// </summary>
    public bool HasMaxAge;

    /// <summary>
    /// The Max-Age duration in seconds when <see cref="HasMaxAge"/> is true.
    /// </summary>
    public long MaxAgeSeconds;

    /// <summary>
    /// Indicates whether a Domain attribute was supplied.
    /// </summary>
    public bool HasDomain;

    /// <summary>
    /// The Domain attribute value when <see cref="HasDomain"/> is true.
    /// </summary>
    public string Domain = "";

    /// <summary>
    /// Indicates whether a Path attribute was supplied.
    /// </summary>
    public bool HasPath;

    /// <summary>
    /// The Path attribute value when <see cref="HasPath"/> is true.
    /// </summary>
    public string Path = "";

    /// <summary>
    /// Indicates whether the Secure attribute was supplied.
    /// </summary>
    public bool Secure;

    /// <summary>
    /// Indicates whether the HttpOnly attribute was supplied.
    /// </summary>
    public bool HttpOnly;

    /// <summary>
    /// Indicates whether a SameSite attribute was supplied.
    /// </summary>
    public bool HasSameSite;

    /// <summary>
    /// The SameSite behavior of the cookie.
    /// </summary>
    public CookieSameSiteMode SameSiteMode;

    /// <summary>
    /// Indicates whether the Partitioned attribute was supplied.
    /// </summary>
    public bool Partitioned;

    /// <summary>
    /// The safe unrecognized extension attributes in source order.
    /// </summary>
    public CookieAttribute[] Extensions = [];
}
