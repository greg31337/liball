namespace version;

/// <summary>
/// Provides public construction, membership, and explanation operations for version ranges.
/// </summary>
/// <remarks>
/// This class exposes range behavior while keeping parser and algebra implementation types internal.
/// </remarks>
public class VersionRangeOperations : IVersionRangeOperations
{
    /// <summary>
    /// Initializes a stateless version range operations service.
    /// </summary>
    public VersionRangeOperations()
    {
    }

    /// <summary>
    /// Creates a range that accepts no versions.
    /// </summary>
    /// <returns>The resulting empty version range.</returns>
    public VersionRange CreateEmptyRange()
    {
        return VersionRange.CreateEmpty();
    }

    /// <summary>
    /// Creates a range that accepts every parsed version.
    /// </summary>
    /// <returns>The resulting universal version range.</returns>
    public VersionRange CreateUniversalRange()
    {
        return VersionRange.CreateUniversal();
    }

    /// <summary>
    /// Reports whether a range accepts no versions.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <returns><c>true</c> when the range is empty; otherwise, <c>false</c>.</returns>
    public bool IsEmpty(VersionRange range)
    {
        System.ArgumentNullException.ThrowIfNull(range);
        return range.IsEmpty();
    }

    /// <summary>
    /// Reports whether a range accepts every parsed version.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <returns><c>true</c> when the range is universal; otherwise, <c>false</c>.</returns>
    public bool IsUniversal(VersionRange range)
    {
        System.ArgumentNullException.ThrowIfNull(range);
        return range.IsUniversal();
    }

    /// <summary>
    /// Reports whether a parsed version satisfies a range's comparator sets.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the version satisfies the range; otherwise, <c>false</c>.</returns>
    public bool ContainsVersion(VersionRange range, UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(range);
        System.ArgumentNullException.ThrowIfNull(version);
        return range.ContainsVersion(version);
    }

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
        bool includePrerelease)
    {
        System.ArgumentNullException.ThrowIfNull(range);
        System.ArgumentNullException.ThrowIfNull(version);
        return range.ContainsVersionWithPrerelease(version, includePrerelease);
    }

    /// <summary>
    /// Parses version text and reports whether it satisfies a range using the supplied parse options.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns><c>true</c> when the parsed version satisfies the range; otherwise, <c>false</c>.</returns>
    public bool ContainsText(VersionRange range, string text, VersionParseOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(range);
        return range.ContainsTextWithOptions(text, options);
    }

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
        bool includePrerelease)
    {
        System.ArgumentNullException.ThrowIfNull(range);
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(options);

        VersionParseResult result = VersionOptionDispatcher.Parse(text, options);
        if (result.IsFailure())
        {
            return false;
        }

        return range.ContainsVersionWithPrerelease(result.GetVersion(), includePrerelease);
    }

    /// <summary>
    /// Explains which comparator set accepted a parsed version or which comparator rejected it.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting structured match explanation.</returns>
    public VersionMatchExplanation ExplainMatch(VersionRange range, UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(range);
        System.ArgumentNullException.ThrowIfNull(version);
        return range.Explain(version);
    }

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
        bool includePrerelease)
    {
        System.ArgumentNullException.ThrowIfNull(range);
        System.ArgumentNullException.ThrowIfNull(version);

        if (range.ContainsVersionWithPrerelease(version, includePrerelease))
        {
            return range.Explain(version);
        }

        if (version.IsPrerelease() && !includePrerelease && range.ContainsVersion(version))
        {
            return VersionMatchExplanation.CreateFailure(
                version,
                -1,
                null,
                "Prerelease version is not admitted by range.");
        }

        return range.Explain(version);
    }

    /// <summary>
    /// Returns the intersection that accepts only versions admitted by both ranges.
    /// </summary>
    /// <param name="left">The left range used by the operation.</param>
    /// <param name="right">The right range used by the operation.</param>
    /// <returns>The resulting intersected version range.</returns>
    public VersionRange IntersectRanges(VersionRange left, VersionRange right)
    {
        return VersionRangeAlgebra.Intersect(left, right);
    }

    /// <summary>
    /// Returns the union that accepts versions admitted by either range.
    /// </summary>
    /// <param name="left">The left range used by the operation.</param>
    /// <param name="right">The right range used by the operation.</param>
    /// <returns>The resulting union version range.</returns>
    public VersionRange UnionRanges(VersionRange left, VersionRange right)
    {
        return VersionRangeAlgebra.Union(left, right);
    }

    /// <summary>
    /// Returns the difference that accepts versions in the left range but not the right range.
    /// </summary>
    /// <param name="left">The left range used by the operation.</param>
    /// <param name="right">The right range used by the operation.</param>
    /// <returns>The resulting difference version range.</returns>
    public VersionRange DifferenceRanges(VersionRange left, VersionRange right)
    {
        return VersionRangeAlgebra.Difference(left, right);
    }

    /// <summary>
    /// Returns a range that accepts exactly the versions rejected by the supplied range.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <returns>The resulting complemented version range.</returns>
    public VersionRange ComplementRange(VersionRange range)
    {
        return VersionRangeAlgebra.Complement(range);
    }

    /// <summary>
    /// Removes contradictory and redundant comparator clauses without changing membership.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <returns>The resulting simplified version range.</returns>
    public VersionRange SimplifyRange(VersionRange range)
    {
        return VersionRangeAlgebra.Simplify(range);
    }

    /// <summary>
    /// Builds an equivalent range for the supplied finite candidate set.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine in array order.</param>
    /// <returns>The resulting finite-candidate version range.</returns>
    public VersionRange SimplifyRangeAgainstCandidates(
        VersionRange range,
        UniversalVersion[] candidates)
    {
        return VersionRangeAlgebra.SimplifyAgainstCandidates(range, candidates);
    }

    /// <summary>
    /// Reports whether no version can satisfy a range.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <returns><c>true</c> when the range is unsatisfiable; otherwise, <c>false</c>.</returns>
    public bool IsUnsatisfiable(VersionRange range)
    {
        return VersionRangeAlgebra.IsUnsatisfiable(range);
    }

    /// <summary>
    /// Reports whether every version accepted by the left range is accepted by the right range.
    /// </summary>
    /// <param name="left">The left range used by the operation.</param>
    /// <param name="right">The right range used by the operation.</param>
    /// <returns><c>true</c> when the left range is a subset of the right range; otherwise, <c>false</c>.</returns>
    public bool IsSubset(VersionRange left, VersionRange right)
    {
        return VersionRangeAlgebra.IsSubset(left, right);
    }

    /// <summary>
    /// Reports whether every version accepted by the right range is accepted by the left range.
    /// </summary>
    /// <param name="left">The left range used by the operation.</param>
    /// <param name="right">The right range used by the operation.</param>
    /// <returns><c>true</c> when the left range is a superset of the right range; otherwise, <c>false</c>.</returns>
    public bool IsSuperset(VersionRange left, VersionRange right)
    {
        return VersionRangeAlgebra.IsSuperset(left, right);
    }

    /// <summary>
    /// Reports whether the ranges accept no version in common.
    /// </summary>
    /// <param name="left">The left range used by the operation.</param>
    /// <param name="right">The right range used by the operation.</param>
    /// <returns><c>true</c> when the ranges are disjoint; otherwise, <c>false</c>.</returns>
    public bool IsDisjoint(VersionRange left, VersionRange right)
    {
        return VersionRangeAlgebra.IsDisjoint(left, right);
    }

    /// <summary>
    /// Formats a version range in normalized text for the requested range syntax.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="syntax">The range syntax used for the normalized text.</param>
    /// <returns>The resulting normalized range text.</returns>
    public string FormatNormalizedRange(VersionRange range, RangeSyntax syntax)
    {
        return VersionRangeNormalizedFormatter.Format(range, syntax);
    }

    /// <summary>
    /// Gets the lowest represented version admitted by a range when one exists.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="syntax">The range syntax that determines version comparison semantics.</param>
    /// <returns>A result containing the minimum version or an explicit no-value state.</returns>
    public VersionRangeMinimumResult GetMinimumPossibleVersion(
        VersionRange range,
        RangeSyntax syntax)
    {
        return VersionRangeMinimumFinder.Find(range, syntax);
    }
}
