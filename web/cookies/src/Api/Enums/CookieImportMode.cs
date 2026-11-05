namespace cookies;

/// <summary>
/// Specifies how imported cookies affect existing stored cookies.
/// </summary>
public enum CookieImportMode
{
    /// <summary>
    /// Merges imported cookies into the current store.
    /// </summary>
    Merge = 0,

    /// <summary>
    /// Replaces the current store with imported cookies.
    /// </summary>
    Replace = 1
}
