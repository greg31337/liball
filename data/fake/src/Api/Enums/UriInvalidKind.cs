namespace fake;

/// <summary>
/// Selects a deliberate URI or URL syntax defect.
/// </summary>
public enum UriInvalidKind
{
    /// <summary>
    /// Uses a scheme that begins with a digit.
    /// </summary>
    Scheme = 0,

    /// <summary>
    /// Uses a host label with a leading hyphen.
    /// </summary>
    Host = 1,

    /// <summary>
    /// Uses an incomplete percent-encoded octet.
    /// </summary>
    PercentEncoding = 2,

    /// <summary>
    /// Places an unencoded space in the path.
    /// </summary>
    Whitespace = 3,

    /// <summary>
    /// Omits the colon between the scheme and authority.
    /// </summary>
    Authority = 4,

    /// <summary>
    /// Places an extra user-information delimiter in the authority.
    /// </summary>
    UserInfo = 5
}
