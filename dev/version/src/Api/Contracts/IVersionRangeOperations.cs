namespace version;

/// <summary>
/// Defines version range construction, membership, and match explanation operations.
/// </summary>
public interface IVersionRangeOperations
{
    /// <summary>
    /// Creates a range that accepts no versions.
    /// </summary>
    /// <returns>The resulting empty version range.</returns>
    public VersionRange CreateEmptyRange();

    /// <summary>
    /// Creates a range that accepts every parsed version.
    /// </summary>
    /// <returns>The resulting universal version range.</returns>
    public VersionRange CreateUniversalRange();

    /// <summary>
    /// Reports whether a range accepts no versions.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <returns><c>true</c> when the range is empty; otherwise, <c>false</c>.</returns>
    public bool IsEmpty(VersionRange range);

    /// <summary>
    /// Reports whether a range accepts every parsed version.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <returns><c>true</c> when the range is universal; otherwise, <c>false</c>.</returns>
    public bool IsUniversal(VersionRange range);

    /// <summary>
    /// Reports whether a parsed version satisfies a range's comparator sets.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the version satisfies the range; otherwise, <c>false</c>.</returns>
    public bool ContainsVersion(VersionRange range, UniversalVersion version);

    /// <summary>
    /// Reports whether a parsed version satisfies a range with an explicit prerelease override.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="includePrerelease">Whether prerelease candidates are admitted regardless of explicit range comparators.</param>
    /// <returns><c>true</c> when the version satisfies the range; otherwise, <c>false</c>.</returns>
    public bool ContainsVersionWithPrerelease(
        VersionRange range,
        UniversalVersion version,
        bool includePrerelease);

    /// <summary>
    /// Parses version text and reports whether it satisfies a range using the supplied parse options.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns><c>true</c> when the parsed version satisfies the range; otherwise, <c>false</c>.</returns>
    public bool ContainsText(VersionRange range, string text, VersionParseOptions options);

    /// <summary>
    /// Parses version text and reports whether it satisfies a range with an explicit prerelease override.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="includePrerelease">Whether prerelease candidates are admitted regardless of explicit range comparators.</param>
    /// <returns><c>true</c> when the parsed version satisfies the range; otherwise, <c>false</c>.</returns>
    public bool ContainsTextWithPrerelease(
        VersionRange range,
        string text,
        VersionParseOptions options,
        bool includePrerelease);

    /// <summary>
    /// Explains which comparator set accepted a parsed version or which comparator rejected it.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting structured match explanation.</returns>
    public VersionMatchExplanation ExplainMatch(VersionRange range, UniversalVersion version);

    /// <summary>
    /// Explains a parsed version match with an explicit prerelease override.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="includePrerelease">Whether prerelease candidates are admitted regardless of explicit range comparators.</param>
    /// <returns>The resulting structured match explanation.</returns>
    public VersionMatchExplanation ExplainMatchWithPrerelease(
        VersionRange range,
        UniversalVersion version,
        bool includePrerelease);

    /// <summary>
    /// Returns the intersection that accepts only versions admitted by both ranges.
    /// </summary>
    /// <param name="left">The left range used by the operation.</param>
    /// <param name="right">The right range used by the operation.</param>
    /// <returns>The resulting intersected version range.</returns>
    public VersionRange IntersectRanges(VersionRange left, VersionRange right);

    /// <summary>
    /// Returns the union that accepts versions admitted by either range.
    /// </summary>
    /// <param name="left">The left range used by the operation.</param>
    /// <param name="right">The right range used by the operation.</param>
    /// <returns>The resulting union version range.</returns>
    public VersionRange UnionRanges(VersionRange left, VersionRange right);

    /// <summary>
    /// Returns the difference that accepts versions in the left range but not the right range.
    /// </summary>
    /// <param name="left">The left range used by the operation.</param>
    /// <param name="right">The right range used by the operation.</param>
    /// <returns>The resulting difference version range.</returns>
    public VersionRange DifferenceRanges(VersionRange left, VersionRange right);

    /// <summary>
    /// Returns a range that accepts exactly the versions rejected by the supplied range.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <returns>The resulting complemented version range.</returns>
    public VersionRange ComplementRange(VersionRange range);

    /// <summary>
    /// Removes contradictory and redundant comparator clauses without changing membership.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <returns>The resulting simplified version range.</returns>
    public VersionRange SimplifyRange(VersionRange range);

    /// <summary>
    /// Builds an equivalent range for the supplied finite candidate set.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine in array order.</param>
    /// <returns>The resulting finite-candidate version range.</returns>
    public VersionRange SimplifyRangeAgainstCandidates(
        VersionRange range,
        UniversalVersion[] candidates);

    /// <summary>
    /// Reports whether no version can satisfy a range.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <returns><c>true</c> when the range is unsatisfiable; otherwise, <c>false</c>.</returns>
    public bool IsUnsatisfiable(VersionRange range);

    /// <summary>
    /// Reports whether every version accepted by the left range is accepted by the right range.
    /// </summary>
    /// <param name="left">The left range used by the operation.</param>
    /// <param name="right">The right range used by the operation.</param>
    /// <returns><c>true</c> when the left range is a subset of the right range; otherwise, <c>false</c>.</returns>
    public bool IsSubset(VersionRange left, VersionRange right);

    /// <summary>
    /// Reports whether every version accepted by the right range is accepted by the left range.
    /// </summary>
    /// <param name="left">The left range used by the operation.</param>
    /// <param name="right">The right range used by the operation.</param>
    /// <returns><c>true</c> when the left range is a superset of the right range; otherwise, <c>false</c>.</returns>
    public bool IsSuperset(VersionRange left, VersionRange right);

    /// <summary>
    /// Reports whether the ranges accept no version in common.
    /// </summary>
    /// <param name="left">The left range used by the operation.</param>
    /// <param name="right">The right range used by the operation.</param>
    /// <returns><c>true</c> when the ranges are disjoint; otherwise, <c>false</c>.</returns>
    public bool IsDisjoint(VersionRange left, VersionRange right);

    /// <summary>
    /// Formats a version range in normalized text for the requested range syntax.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="syntax">The range syntax used for the normalized text.</param>
    /// <returns>The resulting normalized range text.</returns>
    public string FormatNormalizedRange(VersionRange range, RangeSyntax syntax);

    /// <summary>
    /// Gets the lowest represented version admitted by a range when one exists.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="syntax">The range syntax that determines version comparison semantics.</param>
    /// <returns>A result containing the minimum version or an explicit no-value state.</returns>
    public VersionRangeMinimumResult GetMinimumPossibleVersion(
        VersionRange range,
        RangeSyntax syntax);
}
