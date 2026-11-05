namespace version;

/// <summary>
/// Defines Python and PEP 440 version, specifier, selection, and range operations.
/// </summary>
public interface IPythonOperations
{
    /// <summary>
    /// Parses a PEP 440 version without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed version or diagnostics.</returns>
    public VersionParseResult ParseVersion(string text);

    /// <summary>
    /// Parses and normalizes PEP 440 version text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The normalized PEP 440 version text.</returns>
    public string NormalizeVersion(string text);

    /// <summary>
    /// Formats a parsed Python version in canonical PEP 440 form.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The canonical version text.</returns>
    public string FormatCanonicalVersion(UniversalVersion version);

    /// <summary>
    /// Formats a parsed Python version for display, preserving its original spelling when available.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The display version text.</returns>
    public string FormatDisplayVersion(UniversalVersion version);

    /// <summary>
    /// Compares two PEP 440 versions.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number.</returns>
    public int CompareVersions(string leftText, string rightText);

    /// <summary>
    /// Parses a PEP 440 specifier set without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed specifier range or diagnostics.</returns>
    public VersionRangeParseResult ParseSpecifier(string text);

    /// <summary>
    /// Reports whether version text satisfies a parsed specifier and prerelease policy.
    /// </summary>
    /// <param name="specifier">The parsed specifier range.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="options">The options that control prerelease admission.</param>
    /// <returns><c>true</c> when the version satisfies the specifier; otherwise, <c>false</c>.</returns>
    public bool SpecifierAllowsVersion(
        VersionRange specifier,
        string versionText,
        VersionSelectionOptions options);

    /// <summary>
    /// Filters finite Python version candidates using a specifier and prerelease policy.
    /// </summary>
    /// <param name="specifier">The parsed specifier range.</param>
    /// <param name="versionTexts">The version texts to examine.</param>
    /// <param name="options">The options that control prerelease admission.</param>
    /// <returns>A new array containing accepted original candidate texts in input order.</returns>
    public string[] FilterCandidates(
        VersionRange specifier,
        string[] versionTexts,
        VersionSelectionOptions options);

    /// <summary>
    /// Selects the highest finite Python version candidate using a specifier and prerelease policy.
    /// </summary>
    /// <param name="specifier">The parsed specifier range.</param>
    /// <param name="versionTexts">The version texts to examine.</param>
    /// <param name="options">The options that control prerelease admission.</param>
    /// <returns>The original selected candidate text, or null when no candidate matches.</returns>
    public string? SelectHighestCandidate(
        VersionRange specifier,
        string[] versionTexts,
        VersionSelectionOptions options);

    /// <summary>
    /// Reports whether every version accepted by the left specifier is accepted by the right specifier.
    /// </summary>
    /// <param name="left">The left specifier range.</param>
    /// <param name="right">The right specifier range.</param>
    /// <returns><c>true</c> when the left range is a subset of the right range; otherwise, <c>false</c>.</returns>
    public bool IsSpecifierSubset(VersionRange left, VersionRange right);

    /// <summary>
    /// Reports whether every version accepted by the right specifier is accepted by the left specifier.
    /// </summary>
    /// <param name="left">The left specifier range.</param>
    /// <param name="right">The right specifier range.</param>
    /// <returns><c>true</c> when the left range is a superset of the right range; otherwise, <c>false</c>.</returns>
    public bool IsSpecifierSuperset(VersionRange left, VersionRange right);

    /// <summary>
    /// Reports whether two specifier ranges accept no version in common.
    /// </summary>
    /// <param name="left">The left specifier range.</param>
    /// <param name="right">The right specifier range.</param>
    /// <returns><c>true</c> when the ranges are disjoint; otherwise, <c>false</c>.</returns>
    public bool AreSpecifiersDisjoint(VersionRange left, VersionRange right);
}
