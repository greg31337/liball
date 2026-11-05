namespace version;

/// <summary>
/// Provides an injectable instance API over the shared parsing, comparison, formatting, inspection, range, and selection operations.
/// </summary>
/// <remarks>
/// The service is stateless, and consumers can depend on any of the focused interfaces implemented by this class.
/// </remarks>
public class VersionService : IVersionParsing, IVersionRangeParsing, IVersionComparison, IVersionSelection, IVersionFormatting, IVersionInspection
{
    /// <summary>
    /// Initializes a stateless version service.
    /// </summary>
    public VersionService()
    {
    }

    /// <summary>
    /// Parses version text with the supplied options and throws when the text is invalid.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version.</returns>
    public UniversalVersion Parse(string text, VersionParseOptions options)
    {
        return UniversalVersion.Parse(text, options);
    }

    /// <summary>
    /// Attempts to parse version text with the supplied options.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public VersionParseResult TryParse(string text, VersionParseOptions options)
    {
        return UniversalVersion.TryParse(text, options);
    }

    /// <summary>
    /// Parses range text with the supplied options and throws when the text is invalid.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting version range.</returns>
    public VersionRange ParseRange(string text, VersionRangeParseOptions options)
    {
        return VersionRange.Parse(text, options);
    }

    /// <summary>
    /// Attempts to parse range text with the supplied options.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public VersionRangeParseResult TryParseRange(string text, VersionRangeParseOptions options)
    {
        return VersionRange.TryParse(text, options);
    }

    /// <summary>
    /// Parses and compares two version texts with the supplied options.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public int Compare(string leftText, string rightText, VersionComparisonOptions options)
    {
        return UniversalVersion.Compare(leftText, rightText, options);
    }

    /// <summary>
    /// Compares two parsed versions with the supplied options.
    /// </summary>
    /// <param name="left">The left version used by the operation.</param>
    /// <param name="right">The right version used by the operation.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public int CompareVersions(UniversalVersion left, UniversalVersion right, VersionComparisonOptions options)
    {
        return VersionOptionDispatcher.CompareVersions(left, right, options);
    }

    /// <summary>
    /// Sorts parsed candidates in ascending order using the supplied options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new array containing the candidates in ascending order.</returns>
    public UniversalVersion[] SortVersionsAscending(
        UniversalVersion[] candidates,
        VersionComparisonOptions options)
    {
        return VersionCandidateOrdering.SortVersionsAscending(candidates, options);
    }

    /// <summary>
    /// Sorts parsed candidates in descending order using the supplied options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new array containing the candidates in descending order.</returns>
    public UniversalVersion[] SortVersionsDescending(
        UniversalVersion[] candidates,
        VersionComparisonOptions options)
    {
        return VersionCandidateOrdering.SortVersionsDescending(candidates, options);
    }

    /// <summary>
    /// Parses and sorts textual candidates in ascending order using the supplied options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new array containing the candidate texts in ascending order.</returns>
    public string[] SortTextsAscending(string[] candidates, VersionComparisonOptions options)
    {
        return VersionCandidateOrdering.SortTextsAscending(candidates, options);
    }

    /// <summary>
    /// Parses and sorts textual candidates in descending order using the supplied options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new array containing the candidate texts in descending order.</returns>
    public string[] SortTextsDescending(string[] candidates, VersionComparisonOptions options)
    {
        return VersionCandidateOrdering.SortTextsDescending(candidates, options);
    }

    /// <summary>
    /// Filters candidates by range membership and selection options.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    public UniversalVersion[] Filter(VersionRange range, UniversalVersion[] candidates, VersionSelectionOptions options)
    {
        return VersionSelector.Filter(range, candidates, options);
    }

    /// <summary>
    /// Selects the lowest candidate accepted by the range and options.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version, or null when no candidate matches.</returns>
    public UniversalVersion? MinSatisfying(VersionRange range, UniversalVersion[] candidates, VersionSelectionOptions options)
    {
        return VersionSelector.MinSatisfying(range, candidates, options);
    }

    /// <summary>
    /// Selects the highest candidate accepted by the range and options.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version, or null when no candidate matches.</returns>
    public UniversalVersion? MaxSatisfying(VersionRange range, UniversalVersion[] candidates, VersionSelectionOptions options)
    {
        return VersionSelector.MaxSatisfying(range, candidates, options);
    }

    /// <summary>
    /// Selects the preferred stable candidate accepted by the range and options.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version, or null when no candidate matches.</returns>
    public UniversalVersion? BestSatisfying(VersionRange range, UniversalVersion[] candidates, VersionSelectionOptions options)
    {
        return VersionSelector.BestSatisfying(range, candidates, options);
    }

    /// <summary>
    /// Selects the minimum parsed candidate using the supplied comparison options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The minimum candidate, or null when the input is empty.</returns>
    public UniversalVersion? MinimumVersion(
        UniversalVersion[] candidates,
        VersionComparisonOptions options)
    {
        return VersionCandidateOrdering.MinimumVersion(candidates, options);
    }

    /// <summary>
    /// Selects the maximum parsed candidate using the supplied comparison options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The maximum candidate, or null when the input is empty.</returns>
    public UniversalVersion? MaximumVersion(
        UniversalVersion[] candidates,
        VersionComparisonOptions options)
    {
        return VersionCandidateOrdering.MaximumVersion(candidates, options);
    }

    /// <summary>
    /// Parses and selects the minimum textual candidate using the supplied comparison options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The minimum candidate text, or null when the input is empty.</returns>
    public string? MinimumText(string[] candidates, VersionComparisonOptions options)
    {
        return VersionCandidateOrdering.MinimumText(candidates, options);
    }

    /// <summary>
    /// Parses and selects the maximum textual candidate using the supplied comparison options.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The maximum candidate text, or null when the input is empty.</returns>
    public string? MaximumText(string[] candidates, VersionComparisonOptions options)
    {
        return VersionCandidateOrdering.MaximumText(candidates, options);
    }

    /// <summary>
    /// Formats the supplied value as original text.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public string FormatOriginal(UniversalVersion version)
    {
        return VersionFormatter.FormatOriginal(version);
    }

    /// <summary>
    /// Formats the supplied value as normalized text.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public string FormatNormalized(UniversalVersion version)
    {
        return VersionFormatter.FormatNormalized(version);
    }

    /// <summary>
    /// Formats the supplied value as canonical text.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public string FormatCanonical(UniversalVersion version)
    {
        return VersionFormatter.FormatCanonical(version);
    }

    /// <summary>
    /// Formats the supplied value as display text.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public string FormatDisplay(UniversalVersion version)
    {
        return VersionFormatter.FormatDisplay(version);
    }

    /// <summary>
    /// Gets the original version text.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public string GetOriginalText(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.GetOriginalText();
    }

    /// <summary>
    /// Gets the dialect that produced the version.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting version dialect.</returns>
    public VersionDialect GetDialect(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.GetDialect();
    }

    /// <summary>
    /// Reports whether the version is stable.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the version is stable; otherwise, <c>false</c>.</returns>
    public bool IsStable(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return !version.IsPrerelease();
    }

    /// <summary>
    /// Reports whether the version is a prerelease.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the version is a prerelease; otherwise, <c>false</c>.</returns>
    public bool IsPrerelease(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.IsPrerelease();
    }

    /// <summary>
    /// Reports whether the version has build metadata.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the version has build metadata; otherwise, <c>false</c>.</returns>
    public bool HasBuildMetadata(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.HasBuildMetadata();
    }

    /// <summary>
    /// Gets the number of numeric components.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The requested numeric value.</returns>
    public int GetNumericComponentCount(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.GetNumericComponentCount();
    }

    /// <summary>
    /// Gets a numeric component by zero-based index.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="componentIndex">The zero-based component index.</param>
    /// <returns>The resulting text.</returns>
    public string GetNumericComponent(UniversalVersion version, int componentIndex)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.GetNumericComponent(componentIndex);
    }

    /// <summary>
    /// Gets all numeric components.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change the supplied version.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>A new array containing the numeric components.</returns>
    public string[] GetNumericComponents(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.GetNumericComponents();
    }

    /// <summary>
    /// Gets the number of prerelease identifiers.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The requested numeric value.</returns>
    public int GetPrereleaseIdentifierCount(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.GetPrereleaseIdentifierCount();
    }

    /// <summary>
    /// Gets a prerelease identifier by zero-based index.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="identifierIndex">The zero-based identifier index.</param>
    /// <returns>The resulting version identifier.</returns>
    public VersionIdentifier GetPrereleaseIdentifier(UniversalVersion version, int identifierIndex)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.GetPrereleaseIdentifier(identifierIndex);
    }

    /// <summary>
    /// Gets all prerelease identifiers.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change the supplied version.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>A new array containing the prerelease identifiers.</returns>
    public VersionIdentifier[] GetPrereleaseIdentifiers(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.GetPrereleaseIdentifiers();
    }

    /// <summary>
    /// Gets the number of build identifiers.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The requested numeric value.</returns>
    public int GetBuildIdentifierCount(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.GetBuildIdentifierCount();
    }

    /// <summary>
    /// Gets a build identifier by zero-based index.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="identifierIndex">The zero-based identifier index.</param>
    /// <returns>The resulting version identifier.</returns>
    public VersionIdentifier GetBuildIdentifier(UniversalVersion version, int identifierIndex)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.GetBuildIdentifier(identifierIndex);
    }

    /// <summary>
    /// Gets all build identifiers.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change the supplied version.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>A new array containing the build identifiers.</returns>
    public VersionIdentifier[] GetBuildIdentifiers(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.GetBuildIdentifiers();
    }

    /// <summary>
    /// Gets the number of numeric components omitted by shorthand input.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The requested numeric value.</returns>
    public int GetMissingComponentCount(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.GetMissingComponentCount();
    }

    /// <summary>
    /// Reports whether the original version used a leading <c>v</c> prefix.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the original version used a leading <c>v</c>; otherwise, <c>false</c>.</returns>
    public bool HasLeadingVPrefix(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.HasLeadingVPrefix();
    }

    /// <summary>
    /// Reports whether the version has a revision component.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the version has a revision component; otherwise, <c>false</c>.</returns>
    public bool HasRevisionComponent(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.HasRevisionComponent();
    }

    /// <summary>
    /// Gets the revision component, or an empty string when no revision is present.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public string GetRevisionComponent(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.GetRevisionComponent();
    }

    /// <summary>
    /// Reports whether the version has Python-specific metadata.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the version has Python-specific metadata; otherwise, <c>false</c>.</returns>
    public bool HasPythonVersion(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.HasPythonVersion();
    }

    /// <summary>
    /// Gets the Python-specific version metadata.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting Python version.</returns>
    /// <exception cref="System.InvalidOperationException">The supplied version has no Python-specific metadata.</exception>
    public PythonVersion GetPythonVersion(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        return version.GetPythonVersion();
    }

    /// <summary>
    /// Reports whether both versions have equal precedence, ignoring identity-only build metadata.
    /// </summary>
    /// <param name="left">The left version used by the operation.</param>
    /// <param name="right">The right version used by the operation.</param>
    /// <returns><c>true</c> when both versions have equal precedence; otherwise, <c>false</c>.</returns>
    public bool EqualsByPrecedence(UniversalVersion left, UniversalVersion right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);
        return left.EqualsByPrecedence(right);
    }

    /// <summary>
    /// Reports whether both versions have the same complete identity, including build metadata.
    /// </summary>
    /// <param name="left">The left version used by the operation.</param>
    /// <param name="right">The right version used by the operation.</param>
    /// <returns><c>true</c> when both versions have the same complete identity; otherwise, <c>false</c>.</returns>
    public bool EqualsByIdentity(UniversalVersion left, UniversalVersion right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);
        return left.EqualsByIdentity(right);
    }

    /// <summary>
    /// Reports whether both versions have the same normalized package identity.
    /// </summary>
    /// <param name="left">The left version used by the operation.</param>
    /// <param name="right">The right version used by the operation.</param>
    /// <returns><c>true</c> when both versions have the same normalized package identity; otherwise, <c>false</c>.</returns>
    public bool EqualsByNormalizedIdentity(UniversalVersion left, UniversalVersion right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);
        return left.EqualsByNormalizedIdentity(right);
    }
}
