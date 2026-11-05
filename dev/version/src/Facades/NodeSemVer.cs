namespace version;

/// <summary>
/// Provides the public entry points for node-semver version operations.
/// </summary>
/// <remarks>
/// Parsing methods return result objects for ordinary invalid input. Comparison, formatting, and resolver helpers apply the rules of this facade's ecosystem.
/// </remarks>
internal static class NodeSemVer
{
    /// <summary>
    /// Removes accepted prefixes and whitespace and returns normalized node-semver text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? Clean(string? text)
    {
        NodeSemVerValidResult result = CleanResult(text);
        if (result.IsInvalid())
        {
            return null;
        }

        return result.GetVersionText();
    }

    /// <summary>
    /// Removes accepted prefixes and whitespace and returns normalized node-semver text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting text.</returns>
    public static string? Clean(string? text, NodeSemVerCleanOptions options)
    {
        NodeSemVerValidResult result = CleanResult(text, options);
        if (result.IsInvalid())
        {
            return null;
        }

        return result.GetVersionText();
    }

    /// <summary>
    /// Cleans version text while enforcing strict node-semver parsing.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? CleanStrict(string? text)
    {
        return Clean(text);
    }

    /// <summary>
    /// Cleans version text using node-semver loose-input rules.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? CleanLoose(string? text)
    {
        NodeSemVerValidResult result = CleanLooseResult(text);
        if (result.IsInvalid())
        {
            return null;
        }

        return result.GetVersionText();
    }

    /// <summary>
    /// Cleans version text and returns either normalized text or validation diagnostics.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static NodeSemVerValidResult CleanResult(string? text)
    {
        if (text == null)
        {
            return NodeSemVerValidResult.CreateInvalidForNull();
        }

        if (NodeSemVerInputGuard.IsTooLong(text))
        {
            return NodeSemVerValidResult.CreateInvalid(text, CreateVersionLengthDiagnostics(text));
        }

        int[] cleanBounds = GetCleanInputBounds(text);
        string cleanText = text.Substring(cleanBounds[0], cleanBounds[1] - cleanBounds[0]);
        return CreateCleanResult(text, cleanBounds[0], SemVerParser.ParseStrict(cleanText));
    }

    /// <summary>
    /// Cleans version text and returns either normalized text or validation diagnostics.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static NodeSemVerValidResult CleanResult(string? text, NodeSemVerCleanOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(options);

        if (options.IsLoose())
        {
            return CleanLooseResult(text);
        }

        return CleanResult(text);
    }

    /// <summary>
    /// Cleans strict version text and returns either normalized text or validation diagnostics.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static NodeSemVerValidResult CleanStrictResult(string? text)
    {
        return CleanResult(text);
    }

    /// <summary>
    /// Cleans loose version text and returns either normalized text or validation diagnostics.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static NodeSemVerValidResult CleanLooseResult(string? text)
    {
        if (text == null)
        {
            return NodeSemVerValidResult.CreateInvalidForNull();
        }

        if (NodeSemVerInputGuard.IsTooLong(text))
        {
            return NodeSemVerValidResult.CreateInvalid(text, CreateVersionLengthDiagnostics(text));
        }

        return CreateCleanResult(text, 0, SemVerParser.ParseLoose(text));
    }

    /// <summary>
    /// Validates strict node-semver text and returns its normalized form when accepted.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? Valid(string? text)
    {
        NodeSemVerValidResult result = ValidResult(text);
        if (result.IsInvalid())
        {
            return null;
        }

        return result.GetVersionText();
    }

    /// <summary>
    /// Validates loose node-semver text and returns its normalized form when accepted.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? ValidLoose(string? text)
    {
        NodeSemVerValidResult result = ValidLooseResult(text);
        if (result.IsInvalid())
        {
            return null;
        }

        return result.GetVersionText();
    }

    /// <summary>
    /// Validates strict node-semver text and preserves diagnostics for invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static NodeSemVerValidResult ValidResult(string? text)
    {
        if (text == null)
        {
            return NodeSemVerValidResult.CreateInvalidForNull();
        }

        if (NodeSemVerInputGuard.IsTooLong(text))
        {
            return NodeSemVerValidResult.CreateInvalid(text, CreateVersionLengthDiagnostics(text));
        }

        return CreateValidResult(SemVerParser.ParseStrict(text));
    }

    /// <summary>
    /// Validates loose node-semver text and preserves diagnostics for invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static NodeSemVerValidResult ValidLooseResult(string? text)
    {
        if (text == null)
        {
            return NodeSemVerValidResult.CreateInvalidForNull();
        }

        if (NodeSemVerInputGuard.IsTooLong(text))
        {
            return NodeSemVerValidResult.CreateInvalid(text, CreateVersionLengthDiagnostics(text));
        }

        return CreateValidResult(SemVerParser.ParseLoose(text));
    }

    /// <summary>
    /// Parses version text using this API's ecosystem rules.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionParseResult Parse(string? text)
    {
        if (text == null)
        {
            return NodeSemVerInputGuard.CreateVersionNullFailure();
        }

        if (NodeSemVerInputGuard.IsTooLong(text))
        {
            return NodeSemVerInputGuard.CreateVersionLengthFailure(text);
        }

        return SemVerParser.ParseLoose(text);
    }

    /// <summary>
    /// Parses version text using this API's ecosystem rules.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static VersionParseResult Parse(string? text, VersionParseOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(options);

        if (text == null)
        {
            return NodeSemVerInputGuard.CreateVersionNullFailure();
        }

        return VersionOptionDispatcher.Parse(
            text,
            VersionOptionDispatcher.CreateParseOptionsForDialect(options, VersionDialect.NodeSemVer));
    }

    /// <summary>
    /// Extracts the first numeric version tuple and fills omitted minor or patch components with zero.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionParseResult Coerce(string? text)
    {
        if (text == null)
        {
            return NodeSemVerInputGuard.CreateVersionNullFailure();
        }

        if (NodeSemVerInputGuard.IsTooLong(text))
        {
            return NodeSemVerInputGuard.CreateVersionLengthFailure(text);
        }

        return SemVerParser.Coerce(text);
    }

    /// <summary>
    /// Extracts the right-most numeric version tuple without selecting a shorter tuple that shares its ending position.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionParseResult CoerceRightToLeft(string? text)
    {
        if (text == null)
        {
            return NodeSemVerInputGuard.CreateVersionNullFailure();
        }

        if (NodeSemVerInputGuard.IsTooLong(text))
        {
            return NodeSemVerInputGuard.CreateVersionLengthFailure(text);
        }

        return SemVerParser.CoerceRightToLeft(text);
    }

    /// <summary>
    /// Parses a version range from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionRangeParseResult ParseRange(string? text)
    {
        return NodeRangeParser.Parse(text);
    }

    /// <summary>
    /// Parses a version range from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static VersionRangeParseResult ParseRange(string? text, VersionRangeParseOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(options);

        if (text == null)
        {
            return NodeSemVerInputGuard.CreateRangeNullFailure();
        }

        return VersionOptionDispatcher.ParseRange(
            text,
            VersionOptionDispatcher.CreateRangeOptionsForSyntax(options, RangeSyntax.NodeSemVer));
    }

    /// <summary>
    /// Reports whether the supplied version satisfies the node-semver range and prerelease policy.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool Satisfies(string? versionText, string? rangeText)
    {
        VersionSelectionOptions options = VersionSelectionOptions.CreateDefault();
        return Satisfies(versionText, rangeText, options);
    }

    /// <summary>
    /// Reports whether the supplied version satisfies the node-semver range and prerelease policy.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static bool Satisfies(string? versionText, string? rangeText, VersionSelectionOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(options);

        VersionRange? range = ParseRangeOrNull(rangeText);
        UniversalVersion? version = ParseNodeVersionOrNull(versionText);

        if (range == null || version == null)
        {
            return false;
        }

        if (options.GetPrereleasePolicy() == PrereleasePolicy.Include)
        {
            return range.ContainsVersionWithPrerelease(version, true);
        }

        if (options.GetPrereleasePolicy() == PrereleasePolicy.Exclude)
        {
            return !version.IsPrerelease() && range.ContainsVersionWithPrerelease(version, true);
        }

        return range.ContainsVersionWithPrerelease(version, range.AllowsPrereleasesByDefault());
    }

    /// <summary>
    /// Formats this value as comparators.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    public static string[][] ToComparators(string? rangeText)
    {
        VersionRange? range = ParseRangeOrNull(rangeText);
        if (range == null)
        {
            return new string[0][];
        }

        string[][] comparatorSets = new string[range.GetSetCount()][];
        int setIndex = 0;

        while (setIndex < range.GetSetCount())
        {
            VersionComparatorSet set = range.GetSet(setIndex);
            comparatorSets[setIndex] = new string[set.GetComparatorCount()];
            int comparatorIndex = 0;

            while (comparatorIndex < set.GetComparatorCount())
            {
                comparatorSets[setIndex][comparatorIndex] = FormatComparator(set.GetComparator(comparatorIndex));
                comparatorIndex = comparatorIndex + 1;
            }

            setIndex = setIndex + 1;
        }

        return comparatorSets;
    }

    /// <summary>
    /// Returns the lowest representable version admitted by a node-semver range.
    /// </summary>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? MinVersion(string? rangeText)
    {
        VersionRange? range = ParseRangeOrNull(rangeText);
        if (range == null)
        {
            return null;
        }

        UniversalVersion? selected = null;
        int setIndex = 0;

        while (setIndex < range.GetSetCount())
        {
            UniversalVersion? candidate = GetMinimumCandidateForSet(range.GetSet(setIndex));
            if (candidate != null && range.GetSet(setIndex).ContainsVersion(candidate))
            {
                if (selected == null || SemVerVersionComparer.ComparePrecedence(candidate, selected) < 0)
                {
                    selected = candidate;
                }
            }

            setIndex = setIndex + 1;
        }

        if (selected == null)
        {
            return null;
        }

        return selected.ToNormalizedString();
    }

    /// <summary>
    /// Returns normalized range text when the supplied node-semver range is valid.
    /// </summary>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? ValidRange(string? rangeText)
    {
        VersionRange? range = ParseRangeOrNull(rangeText);
        if (range == null)
        {
            return null;
        }

        return FormatRange(range);
    }

    /// <summary>
    /// Reports whether a version lies entirely above or below a node-semver range in the requested direction.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <param name="hilo">The hilo used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public static bool Outside(string? versionText, string? rangeText, string hilo)
    {
        System.ArgumentNullException.ThrowIfNull(hilo);

        if (hilo != ">" && hilo != "<")
        {
            throw new System.ArgumentException("Outside direction must be > or <.", "hilo");
        }

        VersionRange? range = ParseRangeOrNull(rangeText);
        UniversalVersion? version = ParseNodeVersionOrNull(versionText);

        if (range == null || version == null)
        {
            return false;
        }

        if (range.ContainsVersionWithPrerelease(version, true))
        {
            return false;
        }

        if (hilo == ">")
        {
            return IsGreaterThanRange(version, range);
        }

        if (hilo == "<")
        {
            return IsLessThanRange(version, range);
        }

        return false;
    }

    /// <summary>
    /// Reports whether a version is greater than every version admitted by the range.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool Gtr(string? versionText, string? rangeText)
    {
        return Outside(versionText, rangeText, ">");
    }

    /// <summary>
    /// Reports whether a version is less than every version admitted by the range.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool Ltr(string? versionText, string? rangeText)
    {
        return Outside(versionText, rangeText, "<");
    }

    /// <summary>
    /// Reports whether two node-semver ranges admit at least one common version.
    /// </summary>
    /// <param name="leftRangeText">The left range text to process.</param>
    /// <param name="rightRangeText">The right range text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool Intersects(string? leftRangeText, string? rightRangeText)
    {
        VersionRange? left = ParseRangeOrNull(leftRangeText);
        VersionRange? right = ParseRangeOrNull(rightRangeText);

        if (left == null || right == null)
        {
            return false;
        }

        return !VersionRangeAlgebra.IsDisjoint(left, right);
    }

    /// <summary>
    /// Reports whether every version admitted by the first range is admitted by the second range.
    /// </summary>
    /// <param name="subsetRangeText">The subset range text to process.</param>
    /// <param name="supersetRangeText">The superset range text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool Subset(string? subsetRangeText, string? supersetRangeText)
    {
        VersionRange? subset = ParseRangeOrNull(subsetRangeText);
        VersionRange? superset = ParseRangeOrNull(supersetRangeText);

        if (subset == null || superset == null)
        {
            return false;
        }

        return VersionRangeAlgebra.IsSubset(subset, superset);
    }

    /// <summary>
    /// Simplifies a range against a finite candidate list without changing membership for those candidates.
    /// </summary>
    /// <param name="candidateTexts">The candidate texts to process.</param>
    /// <param name="rangeText">The range text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? SimplifyRange(string[] candidateTexts, string? rangeText)
    {
        return SimplifyRange(rangeText, candidateTexts);
    }

    /// <summary>
    /// Simplifies a range against a finite candidate list without changing membership for those candidates.
    /// </summary>
    /// <param name="rangeText">The range text to process.</param>
    /// <param name="candidateTexts">The candidate texts to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static string? SimplifyRange(string? rangeText, string[] candidateTexts)
    {
        System.ArgumentNullException.ThrowIfNull(candidateTexts);

        VersionRange? range = ParseRangeOrNull(rangeText);
        UniversalVersion[]? candidates = ParseCandidateVersions(candidateTexts);

        if (range == null || candidates == null)
        {
            return null;
        }

        VersionRange simplified = VersionRangeAlgebra.SimplifyAgainstCandidates(range, candidates);
        return FormatRange(simplified);
    }

    /// <summary>
    /// Gets a major.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? GetMajor(string? text)
    {
        UniversalVersion? version = ParseAccessorVersion(text);
        if (version == null)
        {
            return null;
        }

        return VersionFormatter.NormalizeNumericText(version.GetNumericComponent(0));
    }

    /// <summary>
    /// Gets a minor.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? GetMinor(string? text)
    {
        UniversalVersion? version = ParseAccessorVersion(text);
        if (version == null)
        {
            return null;
        }

        return VersionFormatter.NormalizeNumericText(version.GetNumericComponent(1));
    }

    /// <summary>
    /// Gets a patch.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? GetPatch(string? text)
    {
        UniversalVersion? version = ParseAccessorVersion(text);
        if (version == null)
        {
            return null;
        }

        return VersionFormatter.NormalizeNumericText(version.GetNumericComponent(2));
    }

    /// <summary>
    /// Gets a prerelease.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    public static string[] GetPrerelease(string? text)
    {
        UniversalVersion? version = ParseAccessorVersion(text);
        if (version == null)
        {
            return new string[0];
        }

        return CopyPrereleaseIdentifierText(version);
    }

    /// <summary>
    /// Gets a prerelease text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? GetPrereleaseText(string? text)
    {
        string[] identifiers = GetPrerelease(text);
        if (identifiers.Length == 0)
        {
            return null;
        }

        return JoinIdentifierText(identifiers);
    }

    /// <summary>
    /// Gets a build metadata.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    public static string[] GetBuildMetadata(string? text)
    {
        UniversalVersion? version = ParseAccessorVersion(text);
        if (version == null)
        {
            return new string[0];
        }

        return CopyBuildIdentifierText(version);
    }

    /// <summary>
    /// Gets a build metadata text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? GetBuildMetadataText(string? text)
    {
        string[] identifiers = GetBuildMetadata(text);
        if (identifiers.Length == 0)
        {
            return null;
        }

        return JoinIdentifierText(identifiers);
    }

    /// <summary>
    /// Increments a version according to the requested release operation.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="releaseType">The release type used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public static string? Increment(string? text, NodeSemVerIncrementReleaseType releaseType)
    {
        return Increment(text, NodeSemVerIncrementOptions.Create(releaseType));
    }

    /// <summary>
    /// Increments a version according to the requested release operation.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static string? Increment(string? text, NodeSemVerIncrementOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(options);

        UniversalVersion? version = ParseIncrementVersion(text, options);
        if (version == null)
        {
            return null;
        }

        if (options.GetReleaseType() == NodeSemVerIncrementReleaseType.Major)
        {
            return IncrementMajor(version);
        }

        if (options.GetReleaseType() == NodeSemVerIncrementReleaseType.Minor)
        {
            return IncrementMinor(version);
        }

        if (options.GetReleaseType() == NodeSemVerIncrementReleaseType.Patch)
        {
            return IncrementPatch(version);
        }

        if (options.GetReleaseType() == NodeSemVerIncrementReleaseType.Premajor)
        {
            return IncrementPremajor(version, options);
        }

        if (options.GetReleaseType() == NodeSemVerIncrementReleaseType.Preminor)
        {
            return IncrementPreminor(version, options);
        }

        if (options.GetReleaseType() == NodeSemVerIncrementReleaseType.Prepatch)
        {
            return IncrementPrepatch(version, options);
        }

        if (options.GetReleaseType() == NodeSemVerIncrementReleaseType.Prerelease)
        {
            return IncrementPrerelease(version, options);
        }

        return IncrementRelease(version);
    }

    /// <summary>
    /// Increments to the next major release and resets lower components.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? Major(string? text)
    {
        return Increment(text, NodeSemVerIncrementReleaseType.Major);
    }

    /// <summary>
    /// Increments to the next minor release and resets the patch component.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? Minor(string? text)
    {
        return Increment(text, NodeSemVerIncrementReleaseType.Minor);
    }

    /// <summary>
    /// Increments to the next patch release.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? Patch(string? text)
    {
        return Increment(text, NodeSemVerIncrementReleaseType.Patch);
    }

    /// <summary>
    /// Increments to the next major line and creates its initial prerelease.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? Premajor(string? text)
    {
        return Increment(text, NodeSemVerIncrementReleaseType.Premajor);
    }

    /// <summary>
    /// Increments to the next major line and creates its initial prerelease.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="identifier">The identifier used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public static string? Premajor(string? text, string identifier)
    {
        return Increment(text, NodeSemVerIncrementOptions.CreateWithIdentifier(NodeSemVerIncrementReleaseType.Premajor, identifier));
    }

    /// <summary>
    /// Increments to the next minor line and creates its initial prerelease.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? Preminor(string? text)
    {
        return Increment(text, NodeSemVerIncrementReleaseType.Preminor);
    }

    /// <summary>
    /// Increments to the next minor line and creates its initial prerelease.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="identifier">The identifier used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public static string? Preminor(string? text, string identifier)
    {
        return Increment(text, NodeSemVerIncrementOptions.CreateWithIdentifier(NodeSemVerIncrementReleaseType.Preminor, identifier));
    }

    /// <summary>
    /// Increments to the next patch and creates its initial prerelease.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? Prepatch(string? text)
    {
        return Increment(text, NodeSemVerIncrementReleaseType.Prepatch);
    }

    /// <summary>
    /// Increments to the next patch and creates its initial prerelease.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="identifier">The identifier used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public static string? Prepatch(string? text, string identifier)
    {
        return Increment(text, NodeSemVerIncrementOptions.CreateWithIdentifier(NodeSemVerIncrementReleaseType.Prepatch, identifier));
    }

    /// <summary>
    /// Advances the current prerelease or starts a prerelease for the next patch.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? Prerelease(string? text)
    {
        return Increment(text, NodeSemVerIncrementReleaseType.Prerelease);
    }

    /// <summary>
    /// Advances the current prerelease or starts a prerelease for the next patch.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="identifier">The identifier used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public static string? Prerelease(string? text, string identifier)
    {
        return Increment(text, NodeSemVerIncrementOptions.CreateWithIdentifier(NodeSemVerIncrementReleaseType.Prerelease, identifier));
    }

    /// <summary>
    /// Removes prerelease identifiers to produce the corresponding stable release.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? Release(string? text)
    {
        return Increment(text, NodeSemVerIncrementReleaseType.Release);
    }

    /// <summary>
    /// Reports whether two versions have equal node-semver precedence.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool Eq(string? leftText, string? rightText)
    {
        return Compare(leftText, rightText) == 0;
    }

    /// <summary>
    /// Reports whether two versions have different node-semver precedence.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool Neq(string? leftText, string? rightText)
    {
        return Compare(leftText, rightText) != 0;
    }

    /// <summary>
    /// Reports whether the left version has greater precedence than the right version.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool Gt(string? leftText, string? rightText)
    {
        return Compare(leftText, rightText) > 0;
    }

    /// <summary>
    /// Reports whether the left version has greater or equal precedence.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool Gte(string? leftText, string? rightText)
    {
        return Compare(leftText, rightText) >= 0;
    }

    /// <summary>
    /// Reports whether the left version has lower precedence than the right version.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool Lt(string? leftText, string? rightText)
    {
        return Compare(leftText, rightText) < 0;
    }

    /// <summary>
    /// Reports whether the left version has lower or equal precedence.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool Lte(string? leftText, string? rightText)
    {
        return Compare(leftText, rightText) <= 0;
    }

    /// <summary>
    /// Evaluates the supplied comparison operator against two versions.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="operatorText">The operator text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public static bool Cmp(string? leftText, string operatorText, string? rightText)
    {
        System.ArgumentNullException.ThrowIfNull(operatorText);

        int comparison = Compare(leftText, rightText);

        if (operatorText == "=" || operatorText == "==" || operatorText == "===")
        {
            return comparison == 0;
        }

        if (operatorText == "!=" || operatorText == "!==")
        {
            return comparison != 0;
        }

        if (operatorText == ">")
        {
            return comparison > 0;
        }

        if (operatorText == ">=")
        {
            return comparison >= 0;
        }

        if (operatorText == "<")
        {
            return comparison < 0;
        }

        if (operatorText == "<=")
        {
            return comparison <= 0;
        }

        throw new System.ArgumentException("Unsupported node-semver comparison operator.", "operatorText");
    }

    /// <summary>
    /// Compares two versions using the selected ecosystem precedence rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public static int Compare(string? leftText, string? rightText)
    {
        UniversalVersion left = ParseStrictVersion(leftText);
        UniversalVersion right = ParseStrictVersion(rightText);
        return SemVerVersionComparer.ComparePrecedence(left, right);
    }

    /// <summary>
    /// Compares two versions using the selected ecosystem precedence rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public static int Compare(string? leftText, string? rightText, VersionComparisonOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(options);

        if (leftText == null || rightText == null)
        {
            throw new System.ArgumentException("Invalid Node SemVer version text.", "text");
        }

        VersionComparisonOptions resolvedOptions = VersionOptionDispatcher.CreateComparisonOptionsForDialect(options, VersionDialect.NodeSemVer);
        return VersionOptionDispatcher.Compare(leftText, rightText, resolvedOptions);
    }

    /// <summary>
    /// Compares two versions in reverse precedence order.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>The requested numeric value.</returns>
    public static int RCompare(string? leftText, string? rightText)
    {
        return 0 - Compare(leftText, rightText);
    }

    /// <summary>
    /// Compares two versions using loose rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public static int CompareLoose(string? leftText, string? rightText)
    {
        UniversalVersion left = ParseLooseVersion(leftText);
        UniversalVersion right = ParseLooseVersion(rightText);
        return SemVerVersionComparer.ComparePrecedence(left, right);
    }

    /// <summary>
    /// Compares two versions using build rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public static int CompareBuild(string? leftText, string? rightText)
    {
        UniversalVersion left = ParseStrictVersion(leftText);
        UniversalVersion right = ParseStrictVersion(rightText);
        return SemVerVersionComparer.CompareIdentity(left, right);
    }

    /// <summary>
    /// Names the first release component that differs between two versions.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? Diff(string? leftText, string? rightText)
    {
        NodeSemVerDiffKind kind = DiffKind(leftText, rightText);

        if (kind == NodeSemVerDiffKind.None)
        {
            return null;
        }

        if (kind == NodeSemVerDiffKind.Major)
        {
            return "major";
        }

        if (kind == NodeSemVerDiffKind.Minor)
        {
            return "minor";
        }

        if (kind == NodeSemVerDiffKind.Patch)
        {
            return "patch";
        }

        if (kind == NodeSemVerDiffKind.Prerelease)
        {
            return "prerelease";
        }

        return "build";
    }

    /// <summary>
    /// Classifies the first release component that differs between two versions.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>The resulting node semver diff kind.</returns>
    public static NodeSemVerDiffKind DiffKind(string? leftText, string? rightText)
    {
        UniversalVersion left = ParseStrictVersion(leftText);
        UniversalVersion right = ParseStrictVersion(rightText);

        if (SemVerVersionComparer.CompareIdentity(left, right) == 0)
        {
            return NodeSemVerDiffKind.None;
        }

        if (left.GetNumericComponent(0) != right.GetNumericComponent(0))
        {
            return NodeSemVerDiffKind.Major;
        }

        if (left.GetNumericComponent(1) != right.GetNumericComponent(1))
        {
            return NodeSemVerDiffKind.Minor;
        }

        if (left.GetNumericComponent(2) != right.GetNumericComponent(2))
        {
            return NodeSemVerDiffKind.Patch;
        }

        if (SemVerVersionComparer.ComparePrecedence(left, right) != 0)
        {
            return NodeSemVerDiffKind.Prerelease;
        }

        return NodeSemVerDiffKind.Build;
    }

    internal static UniversalVersion ParseStrictVersion(string? text)
    {
        VersionParseResult result;

        if (text == null)
        {
            result = NodeSemVerInputGuard.CreateVersionNullFailure();
        }
        else if (NodeSemVerInputGuard.IsTooLong(text))
        {
            result = NodeSemVerInputGuard.CreateVersionLengthFailure(text);
        }
        else
        {
            result = SemVerParser.ParseStrict(text);
        }

        return RequireVersion(result);
    }

    internal static UniversalVersion ParseLooseVersion(string? text)
    {
        VersionParseResult result = Parse(text);
        return RequireVersion(result);
    }

    internal static UniversalVersion RequireVersion(VersionParseResult result)
    {
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid Node SemVer version text.", "text");
        }

        return result.GetVersion();
    }

    internal static UniversalVersion? ParseIncrementVersion(string? text, NodeSemVerIncrementOptions options)
    {
        VersionParseResult result;

        if (options.IsLoose())
        {
            result = Parse(text);
        }
        else if (text == null)
        {
            result = NodeSemVerInputGuard.CreateVersionNullFailure();
        }
        else if (NodeSemVerInputGuard.IsTooLong(text))
        {
            result = NodeSemVerInputGuard.CreateVersionLengthFailure(text);
        }
        else
        {
            result = SemVerParser.ParseStrict(text);
        }

        if (result.IsFailure())
        {
            return null;
        }

        return result.GetVersion();
    }

    internal static UniversalVersion? ParseAccessorVersion(string? text)
    {
        VersionParseResult result;

        if (text == null)
        {
            return null;
        }

        if (NodeSemVerInputGuard.IsTooLong(text))
        {
            return null;
        }

        result = SemVerParser.ParseStrict(text);
        if (result.IsFailure())
        {
            return null;
        }

        return result.GetVersion();
    }

    internal static VersionRange? ParseRangeOrNull(string? rangeText)
    {
        VersionRangeParseResult result = ParseRange(rangeText);
        if (result.IsFailure())
        {
            return null;
        }

        return result.GetRange();
    }

    internal static UniversalVersion? ParseNodeVersionOrNull(string? versionText)
    {
        VersionParseResult result = Parse(versionText);
        if (result.IsFailure())
        {
            return null;
        }

        return result.GetVersion();
    }

    internal static UniversalVersion[]? ParseCandidateVersions(string[] candidateTexts)
    {
        UniversalVersion[] candidates = new UniversalVersion[candidateTexts.Length];
        int index = 0;

        while (index < candidateTexts.Length)
        {
            if (candidateTexts[index] == null)
            {
                return null;
            }

            UniversalVersion? version = ParseNodeVersionOrNull(candidateTexts[index]);
            if (version == null)
            {
                return null;
            }

            candidates[index] = version;
            index = index + 1;
        }

        return candidates;
    }

    internal static string FormatRange(VersionRange range)
    {
        if (range.IsEmpty())
        {
            return "";
        }

        if (range.IsUniversal())
        {
            return "*";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int setIndex = 0;

        while (setIndex < range.GetSetCount())
        {
            if (setIndex > 0)
            {
                builder.Append(" || ");
            }

            builder.Append(FormatComparatorSet(range.GetSet(setIndex)));
            setIndex = setIndex + 1;
        }

        return builder.ToString();
    }

    internal static string FormatComparatorSet(VersionComparatorSet set)
    {
        if (set.GetComparatorCount() == 0)
        {
            return "*";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int comparatorIndex = 0;

        while (comparatorIndex < set.GetComparatorCount())
        {
            if (comparatorIndex > 0)
            {
                builder.Append(' ');
            }

            builder.Append(FormatComparator(set.GetComparator(comparatorIndex)));
            comparatorIndex = comparatorIndex + 1;
        }

        return builder.ToString();
    }

    internal static string FormatComparator(VersionComparator comparator)
    {
        return comparator.GetOperatorText() + comparator.GetVersion().ToNormalizedString();
    }

    internal static UniversalVersion? GetMinimumCandidateForSet(VersionComparatorSet set)
    {
        UniversalVersion? candidate = null;
        int comparatorIndex = 0;

        while (comparatorIndex < set.GetComparatorCount())
        {
            VersionComparator comparator = set.GetComparator(comparatorIndex);
            UniversalVersion? comparatorCandidate = GetMinimumCandidateForComparator(comparator);
            if (comparatorCandidate != null)
            {
                if (candidate == null || SemVerVersionComparer.ComparePrecedence(comparatorCandidate, candidate) > 0)
                {
                    candidate = comparatorCandidate;
                }
            }

            comparatorIndex = comparatorIndex + 1;
        }

        if (candidate == null)
        {
            return VersionRangeAlgebra.GetMinimumRepresentableVersion(VersionDialect.NodeSemVer);
        }

        return candidate;
    }

    internal static UniversalVersion? GetMinimumCandidateForComparator(VersionComparator comparator)
    {
        if (comparator.GetOperator() == RangeOperator.Equal)
        {
            return comparator.GetVersion();
        }

        if (comparator.GetOperator() == RangeOperator.GreaterThanOrEqual)
        {
            return comparator.GetVersion();
        }

        if (comparator.GetOperator() == RangeOperator.GreaterThan)
        {
            string? incremented = Patch(comparator.GetVersion().ToNormalizedString());
            if (incremented == null)
            {
                return null;
            }

            return ParseStrictVersion(incremented);
        }

        return null;
    }

    internal static bool IsGreaterThanRange(UniversalVersion version, VersionRange range)
    {
        int setIndex = 0;

        while (setIndex < range.GetSetCount())
        {
            if (!IsGreaterThanSet(version, range.GetSet(setIndex)))
            {
                return false;
            }

            setIndex = setIndex + 1;
        }

        return true;
    }

    internal static bool IsLessThanRange(UniversalVersion version, VersionRange range)
    {
        int setIndex = 0;

        while (setIndex < range.GetSetCount())
        {
            if (!IsLessThanSet(version, range.GetSet(setIndex)))
            {
                return false;
            }

            setIndex = setIndex + 1;
        }

        return true;
    }

    internal static bool IsGreaterThanSet(UniversalVersion version, VersionComparatorSet set)
    {
        UniversalVersion? upper = null;
        bool upperInclusive = false;
        int comparatorIndex = 0;

        while (comparatorIndex < set.GetComparatorCount())
        {
            VersionComparator comparator = set.GetComparator(comparatorIndex);
            if (IsUpperBoundComparator(comparator))
            {
                if (upper == null || SemVerVersionComparer.ComparePrecedence(comparator.GetVersion(), upper) < 0)
                {
                    upper = comparator.GetVersion();
                    upperInclusive = comparator.GetOperator() == RangeOperator.LessThanOrEqual || comparator.GetOperator() == RangeOperator.Equal;
                }
            }

            comparatorIndex = comparatorIndex + 1;
        }

        if (upper == null)
        {
            return false;
        }

        int comparison = SemVerVersionComparer.ComparePrecedence(version, upper);
        if (upperInclusive)
        {
            return comparison > 0;
        }

        return comparison >= 0;
    }

    internal static bool IsLessThanSet(UniversalVersion version, VersionComparatorSet set)
    {
        UniversalVersion? lower = null;
        bool lowerInclusive = false;
        int comparatorIndex = 0;

        while (comparatorIndex < set.GetComparatorCount())
        {
            VersionComparator comparator = set.GetComparator(comparatorIndex);
            if (IsLowerBoundComparator(comparator))
            {
                if (lower == null || SemVerVersionComparer.ComparePrecedence(comparator.GetVersion(), lower) > 0)
                {
                    lower = comparator.GetVersion();
                    lowerInclusive = comparator.GetOperator() == RangeOperator.GreaterThanOrEqual || comparator.GetOperator() == RangeOperator.Equal;
                }
            }

            comparatorIndex = comparatorIndex + 1;
        }

        if (lower == null)
        {
            return false;
        }

        int comparison = SemVerVersionComparer.ComparePrecedence(version, lower);
        if (lowerInclusive)
        {
            return comparison < 0;
        }

        return comparison <= 0;
    }

    internal static bool IsUpperBoundComparator(VersionComparator comparator)
    {
        return comparator.GetOperator() == RangeOperator.LessThan
            || comparator.GetOperator() == RangeOperator.LessThanOrEqual
            || comparator.GetOperator() == RangeOperator.Equal;
    }

    internal static bool IsLowerBoundComparator(VersionComparator comparator)
    {
        return comparator.GetOperator() == RangeOperator.GreaterThan
            || comparator.GetOperator() == RangeOperator.GreaterThanOrEqual
            || comparator.GetOperator() == RangeOperator.Equal;
    }

    internal static string IncrementMajor(UniversalVersion version)
    {
        string major = version.GetNumericComponent(0);

        if (version.GetNumericComponent(1) != "0" || version.GetNumericComponent(2) != "0" || !version.IsPrerelease())
        {
            major = IncrementNumericText(major);
        }

        return FormatIncrementedVersion(major, "0", "0", new string[0]);
    }

    internal static string IncrementMinor(UniversalVersion version)
    {
        string minor = version.GetNumericComponent(1);

        if (version.GetNumericComponent(2) != "0" || !version.IsPrerelease())
        {
            minor = IncrementNumericText(minor);
        }

        return FormatIncrementedVersion(version.GetNumericComponent(0), minor, "0", new string[0]);
    }

    internal static string IncrementPatch(UniversalVersion version)
    {
        string patch = version.GetNumericComponent(2);

        if (!version.IsPrerelease())
        {
            patch = IncrementNumericText(patch);
        }

        return FormatIncrementedVersion(version.GetNumericComponent(0), version.GetNumericComponent(1), patch, new string[0]);
    }

    internal static string IncrementPremajor(UniversalVersion version, NodeSemVerIncrementOptions options)
    {
        return FormatIncrementedVersion(IncrementNumericText(version.GetNumericComponent(0)), "0", "0", CreateInitialPrerelease(options));
    }

    internal static string IncrementPreminor(UniversalVersion version, NodeSemVerIncrementOptions options)
    {
        return FormatIncrementedVersion(version.GetNumericComponent(0), IncrementNumericText(version.GetNumericComponent(1)), "0", CreateInitialPrerelease(options));
    }

    internal static string IncrementPrepatch(UniversalVersion version, NodeSemVerIncrementOptions options)
    {
        return FormatIncrementedVersion(version.GetNumericComponent(0), version.GetNumericComponent(1), IncrementNumericText(version.GetNumericComponent(2)), CreateInitialPrerelease(options));
    }

    internal static string? IncrementRelease(UniversalVersion version)
    {
        if (!version.IsPrerelease())
        {
            return null;
        }

        return FormatIncrementedVersion(version.GetNumericComponent(0), version.GetNumericComponent(1), version.GetNumericComponent(2), new string[0]);
    }

    internal static string IncrementPrerelease(UniversalVersion version, NodeSemVerIncrementOptions options)
    {
        string patch = version.GetNumericComponent(2);
        string[] prereleaseIdentifiers;

        if (!version.IsPrerelease())
        {
            patch = IncrementNumericText(patch);
            prereleaseIdentifiers = CreateInitialPrerelease(options);
        }
        else
        {
            prereleaseIdentifiers = IncrementPrereleaseIdentifiers(version, options);
        }

        return FormatIncrementedVersion(version.GetNumericComponent(0), version.GetNumericComponent(1), patch, prereleaseIdentifiers);
    }

    internal static string[] CreateInitialPrerelease(NodeSemVerIncrementOptions options)
    {
        if (options.HasIdentifier())
        {
            if (options.IsIdentifierBaseEnabled())
            {
                return new string[] { options.GetIdentifier(), options.GetIdentifierBase().ToString() };
            }

            return new string[] { options.GetIdentifier() };
        }

        return new string[] { options.GetIdentifierBase().ToString() };
    }

    internal static string[] IncrementPrereleaseIdentifiers(UniversalVersion version, NodeSemVerIncrementOptions options)
    {
        if (options.HasIdentifier())
        {
            if (version.GetPrereleaseIdentifier(0).GetText() != options.GetIdentifier())
            {
                return CreateInitialPrerelease(options);
            }
        }

        string[] identifiers = CopyPrereleaseIdentifierText(version);
        int numericIndex = FindLastNumericIdentifier(version);

        if (numericIndex >= 0)
        {
            identifiers[numericIndex] = IncrementNumericText(identifiers[numericIndex]);
            return identifiers;
        }

        string[] appended = new string[identifiers.Length + 1];
        int index = 0;
        while (index < identifiers.Length)
        {
            appended[index] = identifiers[index];
            index = index + 1;
        }

        if (options.IsIdentifierBaseEnabled())
        {
            appended[identifiers.Length] = options.GetIdentifierBase().ToString();
        }
        else
        {
            appended[identifiers.Length] = "0";
        }

        return appended;
    }

    internal static string[] CopyPrereleaseIdentifierText(UniversalVersion version)
    {
        string[] identifiers = new string[version.GetPrereleaseIdentifierCount()];
        int index = 0;

        while (index < identifiers.Length)
        {
            identifiers[index] = version.GetPrereleaseIdentifier(index).GetText();
            index = index + 1;
        }

        return identifiers;
    }

    internal static string[] CopyBuildIdentifierText(UniversalVersion version)
    {
        string[] identifiers = new string[version.GetBuildIdentifierCount()];
        int index = 0;

        while (index < identifiers.Length)
        {
            identifiers[index] = version.GetBuildIdentifier(index).GetText();
            index = index + 1;
        }

        return identifiers;
    }

    internal static string JoinIdentifierText(string[] identifiers)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;

        while (index < identifiers.Length)
        {
            if (index > 0)
            {
                builder.Append('.');
            }

            builder.Append(identifiers[index]);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static int FindLastNumericIdentifier(UniversalVersion version)
    {
        int index = version.GetPrereleaseIdentifierCount() - 1;

        while (index >= 0)
        {
            if (version.GetPrereleaseIdentifier(index).IsNumeric())
            {
                return index;
            }

            index = index - 1;
        }

        return -1;
    }

    internal static string FormatIncrementedVersion(string major, string minor, string patch, string[] prereleaseIdentifiers)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(VersionFormatter.NormalizeNumericText(major));
        builder.Append('.');
        builder.Append(VersionFormatter.NormalizeNumericText(minor));
        builder.Append('.');
        builder.Append(VersionFormatter.NormalizeNumericText(patch));

        if (prereleaseIdentifiers.Length > 0)
        {
            builder.Append('-');
            int index = 0;
            while (index < prereleaseIdentifiers.Length)
            {
                if (index > 0)
                {
                    builder.Append('.');
                }

                builder.Append(prereleaseIdentifiers[index]);
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    internal static string IncrementNumericText(string text)
    {
        string normalized = VersionFormatter.NormalizeNumericText(text);
        char[] digits = normalized.ToCharArray();
        int index = digits.Length - 1;

        while (index >= 0)
        {
            if (digits[index] != '9')
            {
                digits[index] = (char)(digits[index] + 1);
                return new string(digits);
            }

            digits[index] = '0';
            index = index - 1;
        }

        return "1" + new string(digits);
    }

    internal static NodeSemVerValidResult CreateValidResult(VersionParseResult parseResult)
    {
        if (parseResult.IsFailure())
        {
            return NodeSemVerValidResult.CreateInvalid(parseResult.GetSourceText(), parseResult.GetDiagnostics());
        }

        return NodeSemVerValidResult.CreateValid(parseResult.GetSourceText(), parseResult.GetVersion().ToNormalizedString());
    }

    internal static NodeSemVerValidResult CreateCleanResult(string sourceText, int diagnosticOffset, VersionParseResult parseResult)
    {
        if (parseResult.IsFailure())
        {
            return NodeSemVerValidResult.CreateInvalid(sourceText, RebaseDiagnostics(parseResult.GetDiagnostics(), sourceText, diagnosticOffset));
        }

        return NodeSemVerValidResult.CreateValid(sourceText, parseResult.GetVersion().ToNormalizedString());
    }

    internal static VersionDiagnostic[] RebaseDiagnostics(VersionDiagnostic[] diagnostics, string sourceText, int diagnosticOffset)
    {
        VersionDiagnostic[] rebased = new VersionDiagnostic[diagnostics.Length];
        int index = 0;

        while (index < diagnostics.Length)
        {
            int diagnosticIndex = diagnosticOffset + diagnostics[index].GetIndex();
            if (diagnosticIndex > sourceText.Length)
            {
                diagnosticIndex = sourceText.Length;
            }

            rebased[index] = VersionDiagnostic.FromIndex(
                diagnostics[index].GetCode(),
                diagnostics[index].GetMessage(),
                sourceText,
                diagnosticIndex);
            index = index + 1;
        }

        return rebased;
    }

    internal static VersionDiagnostic[] CreateVersionLengthDiagnostics(string text)
    {
        VersionDiagnostic[] diagnostics = new VersionDiagnostic[1];
        diagnostics[0] = NodeSemVerInputGuard.CreateVersionLengthDiagnostic(text);
        return diagnostics;
    }

    internal static int[] GetCleanInputBounds(string text)
    {
        int startIndex = 0;
        int endIndex = text.Length;

        while (startIndex < endIndex && VersionChar.IsWhitespace(text[startIndex]))
        {
            startIndex = startIndex + 1;
        }

        while (endIndex > startIndex && VersionChar.IsWhitespace(text[endIndex - 1]))
        {
            endIndex = endIndex - 1;
        }

        if (startIndex < endIndex && text[startIndex] == '=')
        {
            startIndex = startIndex + 1;
        }

        while (startIndex < endIndex && VersionChar.IsWhitespace(text[startIndex]))
        {
            startIndex = startIndex + 1;
        }

        if (startIndex < endIndex && VersionChar.IsV(text[startIndex]))
        {
            startIndex = startIndex + 1;
        }

        while (startIndex < endIndex && VersionChar.IsWhitespace(text[startIndex]))
        {
            startIndex = startIndex + 1;
        }

        return new int[] { startIndex, endIndex };
    }
}
