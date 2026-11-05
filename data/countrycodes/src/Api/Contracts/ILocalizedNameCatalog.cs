namespace countrycodes;

/// <summary>
/// Defines localized-name catalog operations.
/// </summary>
public interface ILocalizedNameCatalog
{
    /// <summary>
    /// Gets all canonical locale tags available in the compiled dataset.
    /// </summary>
    /// <returns>Canonical locale tags in ordinal order.</returns>
    string[] GetSupportedLocales();

    /// <summary>
    /// Determines whether a canonical locale is available.
    /// </summary>
    /// <param name="locale">The canonical locale tag to test.</param>
    /// <returns>True when the locale is available; otherwise false.</returns>
    bool IsLocaleAvailable(string locale);

    /// <summary>
    /// Gets a localized country display name.
    /// </summary>
    /// <param name="countryAlpha2">The country's canonical alpha-2 code.</param>
    /// <param name="locale">The requested locale.</param>
    /// <param name="useFallback">Whether configured parent-locale fallback is allowed.</param>
    /// <returns>The localized display name, or null when none is available.</returns>
    string? GetLocalizedCountryName(
        string countryAlpha2,
        string locale,
        bool useFallback);

    /// <summary>
    /// Gets a localized subdivision display name.
    /// </summary>
    /// <param name="subdivisionCode">The full canonical subdivision code.</param>
    /// <param name="locale">The requested locale.</param>
    /// <param name="useFallback">Whether configured parent-locale fallback is allowed.</param>
    /// <returns>The localized display name, or null when none is available.</returns>
    string? GetLocalizedSubdivisionName(
        string subdivisionCode,
        string locale,
        bool useFallback);

    /// <summary>
    /// Gets a localized UN M49 region display name.
    /// </summary>
    /// <param name="m49Code">The canonical UN M49 region code.</param>
    /// <param name="locale">The requested locale.</param>
    /// <param name="useFallback">Whether configured parent-locale fallback is allowed.</param>
    /// <returns>The localized display name, or null when none is available.</returns>
    string? GetLocalizedRegionName(
        string m49Code,
        string locale,
        bool useFallback);

    /// <summary>
    /// Gets the deterministic configured locale fallback chain.
    /// </summary>
    /// <param name="locale">The canonical locale whose fallback chain is requested.</param>
    /// <returns>Canonical locale tags ordered from the requested locale toward its parents.</returns>
    string[] GetLocaleFallbackChain(string locale);
}
