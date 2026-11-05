namespace unicode;

/// <summary>
/// Configures and creates immutable Unicode identifier profiles.
/// </summary>
public interface IUnicodeIdentifierProfileBuilder
{
    /// <summary>
    /// Selects XID properties when <c>true</c>, or ID properties when <c>false</c>.
    /// </summary>
    /// <param name="useXidProperties">Whether to use XID_Start and XID_Continue.</param>
    /// <returns>This builder.</returns>
    UnicodeIdentifierProfileBuilder SetUseXidProperties(bool useXidProperties);

    /// <summary>
    /// Replaces the ranges that are accepted in addition to the selected ID or XID properties.
    /// </summary>
    /// <param name="ranges">The inclusive Unicode code-point ranges to add.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>ranges</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an array element is <c>null</c>.</exception>
    UnicodeIdentifierProfileBuilder SetAllowedRanges(UnicodeRange[] ranges);

    /// <summary>
    /// Replaces the ranges that are rejected even when another profile rule permits them.
    /// </summary>
    /// <param name="ranges">The inclusive Unicode code-point ranges to reject.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>ranges</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">Thrown when an array element is <c>null</c>.</exception>
    UnicodeIdentifierProfileBuilder SetDeniedRanges(UnicodeRange[] ranges);

    /// <summary>
    /// Selects the profile's normalization requirement and preparation behavior.
    /// </summary>
    /// <param name="policy">The normalization policy.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>policy</c> is not defined.</exception>
    UnicodeIdentifierProfileBuilder SetNormalizationPolicy(UnicodeIdentifierNormalizationPolicy policy);

    /// <summary>
    /// Selects the profile's case-sensitive or caseless comparison behavior.
    /// </summary>
    /// <param name="policy">The case policy.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>policy</c> is not defined.</exception>
    UnicodeIdentifierProfileBuilder SetCasePolicy(UnicodeIdentifierCasePolicy policy);

    /// <summary>
    /// Selects whether otherwise eligible unassigned code points are accepted.
    /// </summary>
    /// <param name="allowUnassignedCodePoints">Whether to accept unassigned code points.</param>
    /// <returns>This builder.</returns>
    UnicodeIdentifierProfileBuilder SetAllowUnassignedCodePoints(bool allowUnassignedCodePoints);

    /// <summary>
    /// Restricts assigned identifier characters to the supplied significant scripts.
    /// </summary>
    /// <param name="scripts">The permitted scripts; Common and Inherited characters remain neutral.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>scripts</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an array value is not defined.</exception>
    UnicodeIdentifierProfileBuilder SetPermittedScripts(UnicodeScript[] scripts);

    /// <summary>
    /// Selects whether Pattern_Syntax and Pattern_White_Space are rejected.
    /// </summary>
    /// <param name="disallowPatternSyntax">Whether Pattern_Syntax is rejected.</param>
    /// <param name="disallowPatternWhiteSpace">Whether Pattern_White_Space is rejected.</param>
    /// <returns>This builder.</returns>
    UnicodeIdentifierProfileBuilder SetPatternRestrictions(bool disallowPatternSyntax, bool disallowPatternWhiteSpace);

    /// <summary>
    /// Creates an independent identifier profile from the current configuration.
    /// </summary>
    /// <returns>The configured identifier profile.</returns>
    UnicodeIdentifierProfile Build();
}
