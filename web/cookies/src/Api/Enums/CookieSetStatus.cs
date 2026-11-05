namespace cookies;

/// <summary>
/// Specifies the result of attempting to store a cookie.
/// </summary>
public enum CookieSetStatus
{
    /// <summary>
    /// A new cookie was stored.
    /// </summary>
    Accepted = 0,

    /// <summary>
    /// An existing cookie was replaced.
    /// </summary>
    Replaced = 1,

    /// <summary>
    /// An existing cookie was deleted.
    /// </summary>
    Deleted = 2,

    /// <summary>
    /// The input produced no storage mutation.
    /// </summary>
    Ignored = 3,

    /// <summary>
    /// The cookie was not accepted.
    /// </summary>
    Rejected = 4
}
