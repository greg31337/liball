namespace version;

/// <summary>
/// Provides public node-semver validation, parsing, comparison, increment, range, sorting, and selection operations.
/// </summary>
/// <remarks>
/// The service is stateless and delegates parsing and range behavior to the library's node-semver engines.
/// </remarks>
public class NodeSemVerOperations : INodeSemVerOperations
{
    /// <summary>
    /// Initializes a stateless node-semver operations service.
    /// </summary>
    public NodeSemVerOperations()
    {
    }

    /// <summary>
    /// Validates version text using the supplied strict or loose option.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing normalized version text or diagnostics.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public NodeSemVerValidResult Valid(string? text, NodeSemVerCleanOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(options);

        if (options.IsLoose())
        {
            return NodeSemVer.ValidLooseResult(text);
        }

        return NodeSemVer.ValidResult(text);
    }

    /// <summary>
    /// Cleans version text using the supplied strict or loose option.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing cleaned version text or diagnostics.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public NodeSemVerValidResult Clean(string? text, NodeSemVerCleanOptions options)
    {
        return NodeSemVer.CleanResult(text, options);
    }

    /// <summary>
    /// Parses version text using node-semver loose-input rules.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public VersionParseResult Parse(string? text)
    {
        return NodeSemVer.Parse(text);
    }

    /// <summary>
    /// Extracts the first coercible numeric version tuple.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public VersionParseResult Coerce(string? text)
    {
        return NodeSemVer.Coerce(text);
    }

    /// <summary>
    /// Extracts the right-most coercible numeric tuple without selecting a shorter tuple that shares its ending position.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public VersionParseResult CoerceRightToLeft(string? text)
    {
        return NodeSemVer.CoerceRightToLeft(text);
    }

    /// <summary>
    /// Gets the normalized major component, or null for invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public string? GetMajor(string? text)
    {
        return NodeSemVer.GetMajor(text);
    }

    /// <summary>
    /// Gets the normalized minor component, or null for invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public string? GetMinor(string? text)
    {
        return NodeSemVer.GetMinor(text);
    }

    /// <summary>
    /// Gets the normalized patch component, or null for invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public string? GetPatch(string? text)
    {
        return NodeSemVer.GetPatch(text);
    }

    /// <summary>
    /// Gets prerelease identifier text, or an empty array when absent or invalid.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A new array containing the identifiers.</returns>
    public string[] GetPrerelease(string? text)
    {
        return NodeSemVer.GetPrerelease(text);
    }

    /// <summary>
    /// Gets joined prerelease text, or null when absent or invalid.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public string? GetPrereleaseText(string? text)
    {
        return NodeSemVer.GetPrereleaseText(text);
    }

    /// <summary>
    /// Gets build metadata identifier text, or an empty array when absent or invalid.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A new array containing the identifiers.</returns>
    public string[] GetBuildMetadata(string? text)
    {
        return NodeSemVer.GetBuildMetadata(text);
    }

    /// <summary>
    /// Gets joined build metadata text, or null when absent or invalid.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public string? GetBuildMetadataText(string? text)
    {
        return NodeSemVer.GetBuildMetadataText(text);
    }

    /// <summary>
    /// Reports whether two strict versions have equal precedence.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the versions have equal precedence; otherwise, <c>false</c>.</returns>
    public bool Equal(string? leftText, string? rightText)
    {
        return NodeSemVer.Eq(leftText, rightText);
    }

    /// <summary>
    /// Reports whether two strict versions have different precedence.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the versions have different precedence; otherwise, <c>false</c>.</returns>
    public bool NotEqual(string? leftText, string? rightText)
    {
        return NodeSemVer.Neq(leftText, rightText);
    }

    /// <summary>
    /// Reports whether the left strict version has greater precedence.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool GreaterThan(string? leftText, string? rightText)
    {
        return NodeSemVer.Gt(leftText, rightText);
    }

    /// <summary>
    /// Reports whether the left strict version has greater or equal precedence.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool GreaterThanOrEqual(string? leftText, string? rightText)
    {
        return NodeSemVer.Gte(leftText, rightText);
    }

    /// <summary>
    /// Reports whether the left strict version has lower precedence.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool LessThan(string? leftText, string? rightText)
    {
        return NodeSemVer.Lt(leftText, rightText);
    }

    /// <summary>
    /// Reports whether the left strict version has lower or equal precedence.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool LessThanOrEqual(string? leftText, string? rightText)
    {
        return NodeSemVer.Lte(leftText, rightText);
    }

    /// <summary>
    /// Evaluates a node-semver primitive comparison operator.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="operatorText">The comparison operator text.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the comparison is satisfied; otherwise, <c>false</c>.</returns>
    public bool CompareByOperator(string? leftText, string operatorText, string? rightText)
    {
        return NodeSemVer.Cmp(leftText, operatorText, rightText);
    }

    /// <summary>
    /// Compares two strict versions using precedence rules.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number.</returns>
    public int Compare(string? leftText, string? rightText)
    {
        return NodeSemVer.Compare(leftText, rightText);
    }

    /// <summary>
    /// Compares two strict versions in reverse precedence order.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number.</returns>
    public int ReverseCompare(string? leftText, string? rightText)
    {
        return NodeSemVer.RCompare(leftText, rightText);
    }

    /// <summary>
    /// Compares two versions using loose-input parsing and precedence rules.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number.</returns>
    public int CompareLoose(string? leftText, string? rightText)
    {
        return NodeSemVer.CompareLoose(leftText, rightText);
    }

    /// <summary>
    /// Compares two strict versions using precedence followed by build metadata identity.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number.</returns>
    public int CompareBuild(string? leftText, string? rightText)
    {
        return NodeSemVer.CompareBuild(leftText, rightText);
    }

    /// <summary>
    /// Names the highest-order difference between two strict versions.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>The difference name, or null when identity is equal.</returns>
    public string? Diff(string? leftText, string? rightText)
    {
        return NodeSemVer.Diff(leftText, rightText);
    }

    /// <summary>
    /// Classifies the highest-order difference between two strict versions.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>The resulting node-semver difference kind.</returns>
    public NodeSemVerDiffKind DiffKind(string? leftText, string? rightText)
    {
        return NodeSemVer.DiffKind(leftText, rightText);
    }

    /// <summary>
    /// Increments version text using the supplied node-semver options.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The incremented version text, or null when the input is invalid.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public string? Increment(string? text, NodeSemVerIncrementOptions options)
    {
        return NodeSemVer.Increment(text, options);
    }

    /// <summary>
    /// Increments to the next major release.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text, or null for invalid input.</returns>
    public string? IncrementMajor(string? text)
    {
        return NodeSemVer.Major(text);
    }

    /// <summary>
    /// Increments to the next minor release.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text, or null for invalid input.</returns>
    public string? IncrementMinor(string? text)
    {
        return NodeSemVer.Minor(text);
    }

    /// <summary>
    /// Increments to the next patch release.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text, or null for invalid input.</returns>
    public string? IncrementPatch(string? text)
    {
        return NodeSemVer.Patch(text);
    }

    /// <summary>
    /// Increments to the next major prerelease line.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text, or null for invalid input.</returns>
    public string? IncrementPremajor(string? text)
    {
        return NodeSemVer.Premajor(text);
    }

    /// <summary>
    /// Increments to the next minor prerelease line.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text, or null for invalid input.</returns>
    public string? IncrementPreminor(string? text)
    {
        return NodeSemVer.Preminor(text);
    }

    /// <summary>
    /// Increments to the next patch prerelease line.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text, or null for invalid input.</returns>
    public string? IncrementPrepatch(string? text)
    {
        return NodeSemVer.Prepatch(text);
    }

    /// <summary>
    /// Increments prerelease identifiers.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text, or null for invalid input.</returns>
    public string? IncrementPrerelease(string? text)
    {
        return NodeSemVer.Prerelease(text);
    }

    /// <summary>
    /// Removes prerelease identifiers to produce a stable release.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text, or null when no release can be produced.</returns>
    public string? IncrementRelease(string? text)
    {
        return NodeSemVer.Release(text);
    }

    /// <summary>
    /// Parses a node-semver range.
    /// </summary>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns>A result containing the produced range or diagnostics.</returns>
    public VersionRangeParseResult ParseRange(string? rangeText)
    {
        return NodeSemVer.ParseRange(rangeText);
    }

    /// <summary>
    /// Reports whether a version satisfies a range under default prerelease behavior.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns><c>true</c> when the range is satisfied; otherwise, <c>false</c>.</returns>
    public bool Satisfies(string? versionText, string? rangeText)
    {
        return NodeSemVer.Satisfies(versionText, rangeText);
    }

    /// <summary>
    /// Reports whether a version satisfies a range using explicit selection options.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns><c>true</c> when the range is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public bool SatisfiesWithOptions(string? versionText, string? rangeText, VersionSelectionOptions options)
    {
        return NodeSemVer.Satisfies(versionText, rangeText, options);
    }

    /// <summary>
    /// Expands a range into comparator text sets.
    /// </summary>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns>A new array containing comparator sets.</returns>
    public string[][] ToComparators(string? rangeText)
    {
        return NodeSemVer.ToComparators(rangeText);
    }

    /// <summary>
    /// Returns the lowest representable version admitted by a range.
    /// </summary>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns>The resulting text, or null when unavailable.</returns>
    public string? MinVersion(string? rangeText)
    {
        return NodeSemVer.MinVersion(rangeText);
    }

    /// <summary>
    /// Returns normalized comparator text for a valid range.
    /// </summary>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns>The resulting text, or null for invalid input.</returns>
    public string? ValidRange(string? rangeText)
    {
        return NodeSemVer.ValidRange(rangeText);
    }

    /// <summary>
    /// Reports whether a version lies outside a range in the requested direction.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <param name="direction">Either <c>&gt;</c> or <c>&lt;</c>.</param>
    /// <returns><c>true</c> when the version is outside; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">The direction is invalid.</exception>
    public bool Outside(string? versionText, string? rangeText, string direction)
    {
        return NodeSemVer.Outside(versionText, rangeText, direction);
    }

    /// <summary>
    /// Reports whether a version is greater than every version admitted by a range.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool GreaterThanRange(string? versionText, string? rangeText)
    {
        return NodeSemVer.Gtr(versionText, rangeText);
    }

    /// <summary>
    /// Reports whether a version is less than every version admitted by a range.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool LessThanRange(string? versionText, string? rangeText)
    {
        return NodeSemVer.Ltr(versionText, rangeText);
    }

    /// <summary>
    /// Reports whether two ranges admit a common version.
    /// </summary>
    /// <param name="leftRangeText">The left range text to process.</param>
    /// <param name="rightRangeText">The right range text to process.</param>
    /// <returns><c>true</c> when the ranges intersect; otherwise, <c>false</c>.</returns>
    public bool Intersects(string? leftRangeText, string? rightRangeText)
    {
        return NodeSemVer.Intersects(leftRangeText, rightRangeText);
    }

    /// <summary>
    /// Reports whether every version admitted by one range is admitted by another.
    /// </summary>
    /// <param name="subsetRangeText">The proposed subset range text.</param>
    /// <param name="supersetRangeText">The proposed superset range text.</param>
    /// <returns><c>true</c> when the subset relation holds; otherwise, <c>false</c>.</returns>
    public bool Subset(string? subsetRangeText, string? supersetRangeText)
    {
        return NodeSemVer.Subset(subsetRangeText, supersetRangeText);
    }

    /// <summary>
    /// Simplifies a range against finite candidate texts.
    /// </summary>
    /// <param name="rangeText">The range text to process.</param>
    /// <param name="candidateTexts">The candidate texts to examine.</param>
    /// <returns>The resulting text, or null for invalid input.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public string? SimplifyRange(string? rangeText, string[] candidateTexts)
    {
        return NodeSemVer.SimplifyRange(rangeText, candidateTexts);
    }

    /// <summary>
    /// Sorts strict version texts in ascending precedence order.
    /// </summary>
    /// <param name="versionTexts">The version texts to examine.</param>
    /// <returns>A new sorted array.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">A candidate is null or invalid.</exception>
    public string[] Sort(string[] versionTexts)
    {
        return SortTexts(versionTexts, false);
    }

    /// <summary>
    /// Sorts strict version texts in descending precedence order.
    /// </summary>
    /// <param name="versionTexts">The version texts to examine.</param>
    /// <returns>A new sorted array.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">A candidate is null or invalid.</exception>
    public string[] ReverseSort(string[] versionTexts)
    {
        return SortTexts(versionTexts, true);
    }

    /// <summary>
    /// Selects the lowest finite candidate satisfying a range under default prerelease behavior.
    /// </summary>
    /// <param name="versionTexts">The version texts to examine.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns>The normalized selected version, or null when none matches.</returns>
    public string? MinSatisfying(string[] versionTexts, string? rangeText)
    {
        return SelectSatisfying(versionTexts, rangeText, new VersionSelectionOptions(), false);
    }

    /// <summary>
    /// Selects the lowest finite candidate satisfying a range using explicit options.
    /// </summary>
    /// <param name="versionTexts">The version texts to examine.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The normalized selected version, or null when none matches.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public string? MinSatisfyingWithOptions(
        string[] versionTexts,
        string? rangeText,
        VersionSelectionOptions options)
    {
        return SelectSatisfying(versionTexts, rangeText, options, false);
    }

    /// <summary>
    /// Selects the highest finite candidate satisfying a range under default prerelease behavior.
    /// </summary>
    /// <param name="versionTexts">The version texts to examine.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns>The normalized selected version, or null when none matches.</returns>
    public string? MaxSatisfying(string[] versionTexts, string? rangeText)
    {
        return SelectSatisfying(versionTexts, rangeText, new VersionSelectionOptions(), true);
    }

    /// <summary>
    /// Selects the highest finite candidate satisfying a range using explicit options.
    /// </summary>
    /// <param name="versionTexts">The version texts to examine.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The normalized selected version, or null when none matches.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public string? MaxSatisfyingWithOptions(
        string[] versionTexts,
        string? rangeText,
        VersionSelectionOptions options)
    {
        return SelectSatisfying(versionTexts, rangeText, options, true);
    }

    internal string[] SortTexts(string[] versionTexts, bool descending)
    {
        System.ArgumentNullException.ThrowIfNull(versionTexts);

        string[] sorted = new string[versionTexts.Length];
        int index = 0;
        while (index < versionTexts.Length)
        {
            if (versionTexts[index] == null)
            {
                throw new System.ArgumentException("Version arrays cannot contain null values.", "versionTexts");
            }

            NodeSemVerValidResult result = NodeSemVer.ValidResult(versionTexts[index]);
            if (result.IsInvalid())
            {
                throw new System.ArgumentException("Version arrays cannot contain invalid values.", "versionTexts");
            }

            sorted[index] = versionTexts[index];
            index = index + 1;
        }

        int outerIndex = 1;
        while (outerIndex < sorted.Length)
        {
            string candidate = sorted[outerIndex];
            int innerIndex = outerIndex - 1;

            while (innerIndex >= 0 && ShouldMove(sorted[innerIndex], candidate, descending))
            {
                sorted[innerIndex + 1] = sorted[innerIndex];
                innerIndex = innerIndex - 1;
            }

            sorted[innerIndex + 1] = candidate;
            outerIndex = outerIndex + 1;
        }

        return sorted;
    }

    internal bool ShouldMove(string leftText, string rightText, bool descending)
    {
        int comparison = NodeSemVer.Compare(leftText, rightText);
        if (descending)
        {
            return comparison < 0;
        }

        return comparison > 0;
    }

    internal string? SelectSatisfying(
        string[] versionTexts,
        string? rangeText,
        VersionSelectionOptions options,
        bool maximum)
    {
        System.ArgumentNullException.ThrowIfNull(versionTexts);
        System.ArgumentNullException.ThrowIfNull(options);

        VersionRange? range = NodeSemVer.ParseRangeOrNull(rangeText);
        if (range == null)
        {
            return null;
        }

        UniversalVersion[] candidates = ParseSelectionCandidates(versionTexts);
        UniversalVersion? selected;

        if (maximum)
        {
            selected = VersionSelector.MaxSatisfying(range, candidates, options);
        }
        else
        {
            selected = VersionSelector.MinSatisfying(range, candidates, options);
        }

        if (selected == null)
        {
            return null;
        }

        return selected.ToNormalizedString();
    }

    internal UniversalVersion[] ParseSelectionCandidates(string[] versionTexts)
    {
        System.Collections.Generic.List<UniversalVersion> candidates = new System.Collections.Generic.List<UniversalVersion>();
        int index = 0;

        while (index < versionTexts.Length)
        {
            VersionParseResult result = NodeSemVer.Parse(versionTexts[index]);
            if (result.IsSuccess())
            {
                candidates.Add(result.GetVersion());
            }

            index = index + 1;
        }

        return candidates.ToArray();
    }
}
