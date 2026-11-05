namespace cookies;

/// <summary>
/// Specifies whether a stored cookie matches a request.
/// </summary>
public enum CookieMatchStatus
{
    /// <summary>
    /// The cookie is included in the request selection.
    /// </summary>
    Included = 0,

    /// <summary>
    /// The cookie is excluded from the request selection.
    /// </summary>
    Excluded = 1
}
