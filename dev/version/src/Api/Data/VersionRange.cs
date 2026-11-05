namespace version;

/// <summary>
/// Represents a union of comparator sets used to decide whether versions satisfy a range.
/// </summary>
/// <remarks>
/// Comparator sets are alternatives: a candidate matches when every comparator in at least one set accepts it. Prerelease admission is tracked separately from numeric bounds.
/// </remarks>
public class VersionRange
{
    /// <summary>
    /// Stores the <c>sets</c> value.
    /// </summary>
    public VersionComparatorSet[] sets;
    /// <summary>
    /// Stores the <c>nuGetFloatingVersion</c> value.
    /// </summary>
    public NuGetFloatingVersion? nuGetFloatingVersion;
    /// <summary>
    /// Stores the <c>prereleasesAllowedByDefault</c> value.
    /// </summary>
    public bool prereleasesAllowedByDefault;

    /// <summary>
    /// Initializes a new Version Range instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="setValues">The set values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public VersionRange(VersionComparatorSet[] setValues)
    {
        System.ArgumentNullException.ThrowIfNull(setValues);

        sets = CopySets(setValues);
        nuGetFloatingVersion = null;
        prereleasesAllowedByDefault = false;
    }

    /// <summary>
    /// Creates an empty.
    /// </summary>
    /// <returns>The resulting version range.</returns>
    internal static VersionRange CreateEmpty()
    {
        return new VersionRange(new VersionComparatorSet[0]);
    }

    /// <summary>
    /// Creates an universal.
    /// </summary>
    /// <returns>The resulting version range.</returns>
    internal static VersionRange CreateUniversal()
    {
        return CreateSingleSet(VersionComparatorSet.CreateUniversal());
    }

    /// <summary>
    /// Creates a single set.
    /// </summary>
    /// <param name="set">The set used by the operation.</param>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static VersionRange CreateSingleSet(VersionComparatorSet set)
    {
        System.ArgumentNullException.ThrowIfNull(set);

        VersionComparatorSet[] sets = new VersionComparatorSet[1];
        sets[0] = set;
        return new VersionRange(sets);
    }

    /// <summary>
    /// Creates a nuget floating.
    /// </summary>
    /// <param name="floatingVersion">The floating version used by the operation.</param>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static VersionRange CreateNuGetFloating(NuGetFloatingVersion floatingVersion)
    {
        System.ArgumentNullException.ThrowIfNull(floatingVersion);

        VersionComparatorSet[] sets = new VersionComparatorSet[1];
        sets[0] = floatingVersion.ToComparatorSet();
        VersionRange range = new VersionRange(sets);
        range.nuGetFloatingVersion = floatingVersion;
        return range;
    }

    /// <summary>
    /// Creates a range with explicit default prerelease admission behavior.
    /// </summary>
    /// <param name="setValues">The set values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="prereleasesAllowedByDefaultValue">Whether prerelease versions are admitted by default.</param>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static VersionRange CreateWithPrereleaseAdmission(
        VersionComparatorSet[] setValues,
        bool prereleasesAllowedByDefaultValue)
    {
        VersionRange range = new VersionRange(setValues);
        range.prereleasesAllowedByDefault = prereleasesAllowedByDefaultValue;
        return range;
    }

    internal static VersionRange CreateWithPrereleaseAdmissionFromRange(
        VersionRange sourceRange,
        bool prereleasesAllowedByDefaultValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceRange);

        VersionRange range = CreateWithPrereleaseAdmission(
            sourceRange.GetSets(),
            prereleasesAllowedByDefaultValue);
        range.nuGetFloatingVersion = sourceRange.nuGetFloatingVersion;
        return range;
    }

    /// <summary>
    /// Creates a single comparator.
    /// </summary>
    /// <param name="comparator">The comparator used by the operation.</param>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static VersionRange CreateSingleComparator(VersionComparator comparator)
    {
        System.ArgumentNullException.ThrowIfNull(comparator);

        return CreateSingleSet(VersionComparatorSet.CreateSingle(comparator));
    }

    /// <summary>
    /// Returns the union that accepts values admitted by either input.
    /// </summary>
    /// <param name="left">The left used by the operation.</param>
    /// <param name="right">The right used by the operation.</param>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static VersionRange Union(VersionRange left, VersionRange right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        return left.UnionWith(right);
    }

    /// <summary>
    /// Parses version text using this API's ecosystem rules.
    /// </summary>
    /// <remarks>
    /// The complete input is interpreted according to the dialect represented by this API.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static VersionRange Parse(string text, VersionRangeParseOptions options)
    {
        VersionRangeParseResult result = VersionOptionDispatcher.ParseRange(text, options);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid version range text.", "text");
        }

        return result.GetRange();
    }

    /// <summary>
    /// Attempts to parse version text using the supplied options.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    internal static VersionRangeParseResult TryParse(string text, VersionRangeParseOptions options)
    {
        return VersionOptionDispatcher.ParseRange(text, options);
    }

    /// <summary>
    /// Gets a set count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetSetCount()
    {
        return sets.Length;
    }

    /// <summary>
    /// Gets a set.
    /// </summary>
    /// <param name="setIndex">The zero-based set index.</param>
    /// <returns>The resulting version comparator set.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal VersionComparatorSet GetSet(int setIndex)
    {
        if (setIndex < 0 || setIndex >= sets.Length)
        {
            throw new System.ArgumentOutOfRangeException("setIndex");
        }

        return sets[setIndex];
    }

    /// <summary>
    /// Gets a sets.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal VersionComparatorSet[] GetSets()
    {
        return CopySets(sets);
    }

    /// <summary>
    /// Reports whether single set.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsSingleSet()
    {
        return sets.Length == 1;
    }

    /// <summary>
    /// Reports whether this range or collection accepts no values.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsEmpty()
    {
        return sets.Length == 0;
    }

    /// <summary>
    /// Reports whether universal.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsUniversal()
    {
        int index = 0;

        while (index < sets.Length)
        {
            if (sets[index].IsUniversal())
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Reports whether prereleases by default.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool AllowsPrereleasesByDefault()
    {
        return prereleasesAllowedByDefault;
    }

    /// <summary>
    /// Checks whether this value contains version.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool ContainsVersion(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        if (nuGetFloatingVersion != null)
        {
            return nuGetFloatingVersion.ContainsVersion(version);
        }

        int index = 0;
        while (index < sets.Length)
        {
            if (sets[index].ContainsVersion(version))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Checks whether this value contains version.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="includePrerelease">Whether to enable include prerelease.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool ContainsVersionWithPrerelease(UniversalVersion version, bool includePrerelease)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        if (nuGetFloatingVersion != null)
        {
            return nuGetFloatingVersion.ContainsVersionWithPrereleaseOverride(version, includePrerelease);
        }

        if (includePrerelease || !version.IsPrerelease())
        {
            return ContainsVersion(version);
        }

        int index = 0;
        while (index < sets.Length)
        {
            if (sets[index].ContainsVersion(version) && ComparatorSetAllowsPrerelease(sets[index], version))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Explains which comparator set accepted the version or which comparator rejected it.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting version match explanation.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal VersionMatchExplanation Explain(UniversalVersion version)
    {
        System.ArgumentNullException.ThrowIfNull(version);

        if (nuGetFloatingVersion != null)
        {
            if (nuGetFloatingVersion.ContainsVersion(version))
            {
                return VersionMatchExplanation.CreateMatch(version, 0);
            }

            return VersionMatchExplanation.CreateFailure(version, 0, null, "Version does not satisfy NuGet floating version.");
        }

        if (sets.Length == 0)
        {
            return VersionMatchExplanation.CreateFailure(version, -1, null, "Range is empty.");
        }

        VersionMatchExplanation? firstFailure = null;
        int index = 0;

        while (index < sets.Length)
        {
            VersionMatchExplanation setExplanation = ExplainSet(version, sets[index], index);
            if (setExplanation.IsMatch())
            {
                return setExplanation;
            }

            if (firstFailure == null)
            {
                firstFailure = setExplanation;
            }

            index = index + 1;
        }

        if (firstFailure == null)
        {
            return VersionMatchExplanation.CreateFailure(version, -1, null, "Version does not satisfy range.");
        }

        return firstFailure;
    }

    internal VersionMatchExplanation ExplainSet(UniversalVersion version, VersionComparatorSet set, int setIndex)
    {
        int comparatorIndex = 0;

        while (comparatorIndex < set.GetComparatorCount())
        {
            VersionComparator comparator = set.GetComparator(comparatorIndex);
            if (!comparator.ContainsVersion(version))
            {
                return VersionMatchExplanation.CreateFailure(
                    version,
                    setIndex,
                    comparator,
                    "Version does not satisfy comparator " + comparator.GetOperatorText() + " " + comparator.GetVersion().GetOriginalText() + ".");
            }

            comparatorIndex = comparatorIndex + 1;
        }

        return VersionMatchExplanation.CreateMatch(version, setIndex);
    }

    /// <summary>
    /// Checks whether this value contains text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool ContainsSemVerText(string text)
    {
        return ContainsText(text, VersionDialect.SemVer);
    }

    /// <summary>
    /// Checks whether this value contains text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool ContainsText(string text, VersionDialect dialect)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        VersionParseResult result = ParseVersionText(text, dialect);
        if (result.IsFailure())
        {
            return false;
        }

        if (dialect == VersionDialect.NodeSemVer || dialect == VersionDialect.Cargo || dialect == VersionDialect.NuGet || dialect == VersionDialect.Python)
        {
            return ContainsVersionWithPrerelease(result.GetVersion(), prereleasesAllowedByDefault);
        }

        return ContainsVersion(result.GetVersion());
    }

    /// <summary>
    /// Checks whether this value contains text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <param name="includePrerelease">Whether to enable include prerelease.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool ContainsTextWithPrerelease(string text, VersionDialect dialect, bool includePrerelease)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        VersionParseResult result = ParseVersionText(text, dialect);
        if (result.IsFailure())
        {
            return false;
        }

        return ContainsVersionWithPrerelease(result.GetVersion(), includePrerelease);
    }

    /// <summary>
    /// Checks whether this value contains text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool ContainsTextWithOptions(string text, VersionParseOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(options);

        VersionParseResult result = VersionOptionDispatcher.Parse(text, options);
        if (result.IsFailure())
        {
            return false;
        }

        if (options.GetPrereleasePolicy() == PrereleasePolicy.Include)
        {
            return ContainsVersionWithPrerelease(result.GetVersion(), true);
        }

        if (options.GetPrereleasePolicy() == PrereleasePolicy.Exclude)
        {
            return !result.GetVersion().IsPrerelease() && ContainsVersionWithPrerelease(result.GetVersion(), true);
        }

        if (options.GetPrereleasePolicy() == PrereleasePolicy.ExplicitOnly)
        {
            return ContainsVersionWithPrerelease(result.GetVersion(), false);
        }

        return ContainsVersionWithPrerelease(result.GetVersion(), prereleasesAllowedByDefault);
    }

    /// <summary>
    /// Returns a range that accepts versions accepted by either range.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal VersionRange UnionWith(VersionRange other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        VersionComparatorSet[] combined = new VersionComparatorSet[sets.Length + other.GetSetCount()];
        int writeIndex = 0;
        int readIndex = 0;

        while (readIndex < sets.Length)
        {
            combined[writeIndex] = sets[readIndex];
            writeIndex = writeIndex + 1;
            readIndex = readIndex + 1;
        }

        readIndex = 0;
        while (readIndex < other.GetSetCount())
        {
            combined[writeIndex] = other.GetSet(readIndex);
            writeIndex = writeIndex + 1;
            readIndex = readIndex + 1;
        }

        return new VersionRange(combined);
    }

    /// <summary>
    /// Returns a range that accepts only versions accepted by both ranges.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns>The resulting version range.</returns>
    internal VersionRange IntersectWith(VersionRange other)
    {
        return VersionRangeAlgebra.Intersect(this, other);
    }

    /// <summary>
    /// Returns a range that accepts this range minus versions accepted by the other range.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns>The resulting version range.</returns>
    internal VersionRange DifferenceWith(VersionRange other)
    {
        return VersionRangeAlgebra.Difference(this, other);
    }

    /// <summary>
    /// Returns a range that accepts exactly the versions rejected by this range.
    /// </summary>
    /// <returns>The resulting version range.</returns>
    internal VersionRange Complement()
    {
        return VersionRangeAlgebra.Complement(this);
    }

    /// <summary>
    /// Reports whether subset of.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsSubsetOf(VersionRange other)
    {
        return VersionRangeAlgebra.IsSubset(this, other);
    }

    /// <summary>
    /// Reports whether superset of.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsSupersetOf(VersionRange other)
    {
        return VersionRangeAlgebra.IsSuperset(this, other);
    }

    /// <summary>
    /// Reports whether disjoint with.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsDisjointWith(VersionRange other)
    {
        return VersionRangeAlgebra.IsDisjoint(this, other);
    }

    /// <summary>
    /// Reports whether unsatisfiable.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsUnsatisfiable()
    {
        return VersionRangeAlgebra.IsUnsatisfiable(this);
    }

    /// <summary>
    /// Removes contradictory and redundant comparator clauses without changing range membership.
    /// </summary>
    /// <returns>The resulting version range.</returns>
    internal VersionRange Simplify()
    {
        return VersionRangeAlgebra.Simplify(this);
    }

    /// <summary>
    /// Builds an equivalent range for the supplied finite candidate set and removes redundant clauses.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting version range.</returns>
    internal VersionRange SimplifyAgainstCandidates(UniversalVersion[] candidates)
    {
        return VersionRangeAlgebra.SimplifyAgainstCandidates(this, candidates);
    }

    /// <summary>
    /// Filters the supplied values using candidates rules.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal UniversalVersion[] FilterCandidates(UniversalVersion[] candidates)
    {
        return VersionSelector.Filter(this, candidates);
    }

    /// <summary>
    /// Filters the supplied values using candidates rules.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal UniversalVersion[] FilterCandidatesWithOptions(UniversalVersion[] candidates, VersionSelectionOptions options)
    {
        return VersionSelector.Filter(this, candidates, options);
    }

    /// <summary>
    /// Selects the lowest candidate that satisfies the range and prerelease policy.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting universal version.</returns>
    internal UniversalVersion? MinSatisfying(UniversalVersion[] candidates)
    {
        return VersionSelector.MinSatisfying(this, candidates);
    }

    /// <summary>
    /// Selects the lowest candidate that satisfies the range and prerelease policy.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version.</returns>
    internal UniversalVersion? MinSatisfyingWithOptions(UniversalVersion[] candidates, VersionSelectionOptions options)
    {
        return VersionSelector.MinSatisfying(this, candidates, options);
    }

    /// <summary>
    /// Selects the highest candidate that satisfies the range and prerelease policy.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting universal version.</returns>
    internal UniversalVersion? MaxSatisfying(UniversalVersion[] candidates)
    {
        return VersionSelector.MaxSatisfying(this, candidates);
    }

    /// <summary>
    /// Selects the highest candidate that satisfies the range and prerelease policy.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version.</returns>
    internal UniversalVersion? MaxSatisfyingWithOptions(UniversalVersion[] candidates, VersionSelectionOptions options)
    {
        return VersionSelector.MaxSatisfying(this, candidates, options);
    }

    /// <summary>
    /// Selects the preferred stable candidate, falling back according to the prerelease policy.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting universal version.</returns>
    internal UniversalVersion? BestSatisfying(UniversalVersion[] candidates)
    {
        return VersionSelector.BestSatisfying(this, candidates);
    }

    /// <summary>
    /// Selects the preferred stable candidate, falling back according to the prerelease policy.
    /// </summary>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version.</returns>
    internal UniversalVersion? BestSatisfyingWithOptions(UniversalVersion[] candidates, VersionSelectionOptions options)
    {
        return VersionSelector.BestSatisfying(this, candidates, options);
    }

    internal static VersionParseResult ParseVersionText(string text, VersionDialect dialect)
    {
        if (dialect == VersionDialect.SemVer)
        {
            return SemVerParser.ParseStrict(text);
        }

        if (dialect == VersionDialect.NodeSemVer)
        {
            return SemVerParser.ParseLoose(text);
        }

        if (dialect == VersionDialect.Cargo)
        {
            return SemVerParser.ParseStrict(text);
        }

        if (dialect == VersionDialect.NuGet)
        {
            return NuGetVersionParser.Parse(text);
        }

        if (dialect == VersionDialect.Maven)
        {
            return MavenVersionParser.Parse(text);
        }

        if (dialect == VersionDialect.RubyGems)
        {
            return RubyGemsVersionParser.ParseUniversal(text);
        }

        if (dialect == VersionDialect.Go)
        {
            return GoVersionParser.Parse(text);
        }

        if (dialect == VersionDialect.Python)
        {
            return PythonVersionParser.Parse(text);
        }

        return VersionParseResult.CreateFailure(
            text,
            dialect,
            VersionDiagnostic.FromIndex(
                "UNSUPPORTED_RANGE_TEXT_DIALECT",
                "Range text matching does not support this version dialect.",
                text,
                0));
    }

    internal static bool ComparatorSetAllowsPrerelease(VersionComparatorSet set, UniversalVersion version)
    {
        int index = 0;

        while (index < set.GetComparatorCount())
        {
            UniversalVersion comparatorVersion = set.GetComparator(index).GetVersion();
            if (comparatorVersion.IsPrerelease() && HasSameCoreVersion(comparatorVersion, version))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool HasSameCoreVersion(UniversalVersion left, UniversalVersion right)
    {
        if (left.GetNumericComponentCount() != right.GetNumericComponentCount())
        {
            return false;
        }

        int index = 0;
        while (index < left.GetNumericComponentCount())
        {
            string leftComponent = VersionFormatter.NormalizeNumericText(left.GetNumericComponent(index));
            string rightComponent = VersionFormatter.NormalizeNumericText(right.GetNumericComponent(index));

            if (leftComponent != rightComponent)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static VersionComparatorSet[] CopySets(VersionComparatorSet[] setValues)
    {
        VersionComparatorSet[] copy = new VersionComparatorSet[setValues.Length];
        int index = 0;

        while (index < setValues.Length)
        {
            if (setValues[index] == null)
            {
                throw new System.ArgumentException("Version ranges cannot contain null comparator sets.");
            }

            copy[index] = setValues[index];
            index = index + 1;
        }

        return copy;
    }
}
