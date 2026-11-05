namespace cookies;

/// <summary>
/// Configures Cookie and Set-Cookie parsing behavior.
/// </summary>
/// <remarks>
/// Parser construction rejects negative limits and settings that weaken mandatory control-character rejection.
/// </remarks>
public class CookieParseOptions
{
    /// <summary>
    /// The standards profile used while parsing.
    /// </summary>
    public CookieComplianceMode ComplianceMode = CookieComplianceMode.Rfc6265;

    /// <summary>
    /// The limits applied to parser input and parsed values.
    /// </summary>
    public CookieLimits Limits = new CookieLimits();

    /// <summary>
    /// Preserves safe unrecognized Set-Cookie attributes.
    /// </summary>
    public bool PreserveUnknownAttributes = true;

    /// <summary>
    /// Reports safe unrecognized attributes as informational issues.
    /// </summary>
    public bool ReportUnknownAttributes = true;

    /// <summary>
    /// Reports repeated recognized attributes as issues.
    /// </summary>
    public bool ReportDuplicateAttributes = true;

    /// <summary>
    /// Reports each recovery performed by compatible parsing.
    /// </summary>
    public bool ReportCompatibilityRecoveries = true;
}
