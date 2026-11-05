namespace cookies;

/// <summary>
/// Specifies the standards profile used for cookie processing.
/// </summary>
public enum CookieComplianceMode
{
    /// <summary>
    /// Applies the RFC 6265 user-agent algorithms.
    /// </summary>
    Rfc6265 = 0,

    /// <summary>
    /// Applies the RFC 6265bis revision 22 algorithms.
    /// </summary>
    Rfc6265Bis = 1,

    /// <summary>
    /// Applies modern safeguards while accepting selected deployed deviations.
    /// </summary>
    Compatible = 2
}
