namespace cookies;

/// <summary>
/// Represents canonical cookie state retained by a cookie store.
/// </summary>
public class StoredCookie
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
    /// The canonical cookie domain.
    /// </summary>
    public string Domain = "";

    /// <summary>
    /// The canonical cookie path.
    /// </summary>
    public string Path = "";

    /// <summary>
    /// Indicates whether the cookie has an expiry instant.
    /// </summary>
    public bool HasExpires;

    /// <summary>
    /// The expiry instant when <see cref="HasExpires"/> is true.
    /// </summary>
    public global::System.DateTimeOffset Expires;

    /// <summary>
    /// The original creation instant retained across replacement.
    /// </summary>
    public global::System.DateTimeOffset CreationTime;

    /// <summary>
    /// The most recent access instant.
    /// </summary>
    public global::System.DateTimeOffset LastAccessTime;

    /// <summary>
    /// Indicates whether the cookie persists beyond the current session.
    /// </summary>
    public bool Persistent;

    /// <summary>
    /// Indicates whether the cookie matches only its exact canonical host.
    /// </summary>
    public bool HostOnly;

    /// <summary>
    /// Indicates whether the cookie is restricted to secure origins.
    /// </summary>
    public bool Secure;

    /// <summary>
    /// Indicates whether the cookie is hidden from non-HTTP APIs.
    /// </summary>
    public bool HttpOnly;

    /// <summary>
    /// Indicates whether a SameSite attribute was supplied.
    /// </summary>
    public bool HasSameSite;

    /// <summary>
    /// The SameSite behavior of the stored cookie.
    /// </summary>
    public CookieSameSiteMode SameSiteMode;

    /// <summary>
    /// Indicates whether the cookie has a partition key.
    /// </summary>
    public bool HasPartitionKey;

    /// <summary>
    /// The canonical partition key when <see cref="HasPartitionKey"/> is true.
    /// </summary>
    public string PartitionKey = "";

    /// <summary>
    /// Indicates that compatible parsing observed an allowed quoted value.
    /// </summary>
    public bool IsValueQuoted;

    /// <summary>
    /// The safe unrecognized extension attributes in source order.
    /// </summary>
    public CookieAttribute[] Extensions = [];

    /// <summary>
    /// The stable sequence number used to break ordering ties.
    /// </summary>
    public long SequenceNumber;
}
