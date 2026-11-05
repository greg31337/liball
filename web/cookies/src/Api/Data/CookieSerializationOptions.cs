namespace cookies;

/// <summary>
/// Configures canonical and compatible cookie serialization behavior.
/// </summary>
/// <remarks>
/// Serializer construction rejects negative limits and incompatible header-splitting settings.
/// </remarks>
public class CookieSerializationOptions
{
    /// <summary>
    /// The standards profile used while validating serialized values.
    /// </summary>
    public CookieComplianceMode ComplianceMode = CookieComplianceMode.Rfc6265;

    /// <summary>
    /// The limits applied to serialized values and request fields.
    /// </summary>
    public CookieLimits Limits = new CookieLimits();

    /// <summary>
    /// Preserves an allowed compatible quoted-value hint instead of canonicalizing it.
    /// </summary>
    public bool PreserveQuotedValues;

    /// <summary>
    /// Includes safe unrecognized extension attributes in Set-Cookie output.
    /// </summary>
    public bool IncludeExtensions = true;

    /// <summary>
    /// Splits request Cookie output at pair boundaries when a header limit is configured.
    /// </summary>
    public bool SplitCookieFields = true;
}
