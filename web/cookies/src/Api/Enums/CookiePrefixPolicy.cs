namespace cookies;

/// <summary>
/// Specifies how invalid cookie name prefixes are handled.
/// </summary>
public enum CookiePrefixPolicy
{
    /// <summary>
    /// Rejects a prefix violation without reporting it as an error.
    /// </summary>
    SilentReject = 0,

    /// <summary>
    /// Rejects a prefix violation and reports an error.
    /// </summary>
    ErrorReject = 1,

    /// <summary>
    /// Disables cookie name prefix enforcement.
    /// </summary>
    Disabled = 2
}
