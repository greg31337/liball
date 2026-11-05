namespace cookies;

/// <summary>
/// Identifies whether a cookie operation is performed by an HTTP API.
/// </summary>
public enum CookieAccessKind
{
    /// <summary>
    /// The operation is performed by an HTTP API.
    /// </summary>
    Http = 0,

    /// <summary>
    /// The operation is performed by a non-HTTP API.
    /// </summary>
    NonHttp = 1
}
