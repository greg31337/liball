namespace version;

/// <summary>
/// Represents a version range algebra.
/// </summary>
/// <remarks>
/// Values are exposed through ordinary methods because the library intentionally avoids properties and external dependencies.
/// </remarks>
internal static class VersionRangeAlgebra
{
    /// <summary>
    /// Returns the intersection that accepts only values admitted by both inputs.
    /// </summary>
    /// <param name="left">The left used by the operation.</param>
    /// <param name="right">The right used by the operation.</param>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static VersionRange Intersect(VersionRange left, VersionRange right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        System.Collections.Generic.List<VersionComparatorSet> sets = new System.Collections.Generic.List<VersionComparatorSet>();
        int leftIndex = 0;

        while (leftIndex < left.GetSetCount())
        {
            int rightIndex = 0;
            while (rightIndex < right.GetSetCount())
            {
                sets.Add(VersionComparatorSet.Intersect(left.GetSet(leftIndex), right.GetSet(rightIndex)));
                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }

        return new VersionRange(sets.ToArray());
    }

    /// <summary>
    /// Returns the union that accepts values admitted by either input.
    /// </summary>
    /// <param name="left">The left used by the operation.</param>
    /// <param name="right">The right used by the operation.</param>
    /// <returns>The resulting version range.</returns>
    public static VersionRange Union(VersionRange left, VersionRange right)
    {
        return VersionRange.Union(left, right);
    }

    /// <summary>
    /// Gets a minimum representable version.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <returns>The resulting universal version.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public static UniversalVersion GetMinimumRepresentableVersion(VersionDialect dialect)
    {
        if (dialect == VersionDialect.SemVer || dialect == VersionDialect.NodeSemVer || dialect == VersionDialect.Cargo)
        {
            return new UniversalVersion(
                "0.0.0",
                dialect,
                new string[] { "0", "0", "0" },
                new VersionIdentifier[0],
                new VersionIdentifier[0],
                0,
                false,
                false,
                "",
                null);
        }

        if (dialect == VersionDialect.NuGet)
        {
            return ParseRequiredVersion(NuGetVersionParser.Parse("0"));
        }

        if (dialect == VersionDialect.Maven)
        {
            return ParseRequiredVersion(MavenVersionParser.Parse("0"));
        }

        if (dialect == VersionDialect.Go)
        {
            return ParseRequiredVersion(GoVersionParser.Parse("v0.0.0"));
        }

        if (dialect == VersionDialect.RubyGems)
        {
            return ParseRequiredVersion(RubyGemsVersionParser.ParseUniversal("0"));
        }

        if (dialect == VersionDialect.Python)
        {
            return ParseRequiredVersion(PythonVersionParser.Parse("0"));
        }

        throw new System.ArgumentOutOfRangeException("dialect");
    }

    /// <summary>
    /// Removes contradictory and redundant comparator clauses without changing range membership.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static VersionRange Simplify(VersionRange range)
    {
        System.ArgumentNullException.ThrowIfNull(range);

        System.Collections.Generic.List<VersionComparatorSet> sets = new System.Collections.Generic.List<VersionComparatorSet>();
        int index = 0;

        while (index < range.GetSetCount())
        {
            if (!IsUnsatisfiable(range.GetSet(index)))
            {
                sets.Add(Simplify(range.GetSet(index)));
            }

            index = index + 1;
        }

        return new VersionRange(sets.ToArray());
    }

    /// <summary>
    /// Builds an equivalent range for the supplied finite candidate set and removes redundant clauses.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <param name="candidates">The candidates to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public static VersionRange SimplifyAgainstCandidates(VersionRange range, UniversalVersion[] candidates)
    {
        System.ArgumentNullException.ThrowIfNull(range);
        System.ArgumentNullException.ThrowIfNull(candidates);

        System.Collections.Generic.List<VersionComparatorSet> sets = new System.Collections.Generic.List<VersionComparatorSet>();
        System.Collections.Generic.List<UniversalVersion> includedCandidates = new System.Collections.Generic.List<UniversalVersion>();
        int index = 0;

        while (index < candidates.Length)
        {
            if (candidates[index] == null)
            {
                throw new System.ArgumentException("Candidate arrays cannot contain null values.");
            }

            if (range.ContainsVersion(candidates[index]) && !ContainsEquivalentCandidate(includedCandidates, candidates[index]))
            {
                includedCandidates.Add(candidates[index]);
                sets.Add(VersionComparatorSet.CreateSingle(
                    VersionComparator.CreateEqual(candidates[index], VersionComparisonMode.Precedence)));
            }

            index = index + 1;
        }

        return new VersionRange(sets.ToArray());
    }

    /// <summary>
    /// Removes contradictory and redundant comparator clauses without changing range membership.
    /// </summary>
    /// <param name="set">The set used by the operation.</param>
    /// <returns>The resulting version comparator set.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static VersionComparatorSet Simplify(VersionComparatorSet set)
    {
        System.ArgumentNullException.ThrowIfNull(set);

        if (IsUnsatisfiable(set))
        {
            return new VersionComparatorSet(new VersionComparator[]
            {
                VersionComparator.CreateGreaterThan(GetMinimumRepresentableVersion(VersionDialect.SemVer), VersionComparisonMode.Precedence),
                VersionComparator.CreateLessThan(GetMinimumRepresentableVersion(VersionDialect.SemVer), VersionComparisonMode.Precedence)
            });
        }

        VersionComparator? equalComparator = GetEqualComparator(set);
        if (equalComparator != null)
        {
            return VersionComparatorSet.CreateSingle(equalComparator);
        }

        VersionRangeAlgebraBounds bounds = GetBounds(set);
        System.Collections.Generic.List<VersionComparator> comparators = new System.Collections.Generic.List<VersionComparator>();

        if (bounds.lowerComparator != null)
        {
            comparators.Add(bounds.lowerComparator);
        }

        if (bounds.upperComparator != null)
        {
            comparators.Add(bounds.upperComparator);
        }

        AddRelevantNotEqualComparators(set, bounds, comparators);

        return new VersionComparatorSet(comparators.ToArray());
    }

    /// <summary>
    /// Returns the range difference that removes the right input from the left input.
    /// </summary>
    /// <param name="left">The left used by the operation.</param>
    /// <param name="right">The right used by the operation.</param>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static VersionRange Difference(VersionRange left, VersionRange right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        return Intersect(left, Complement(right));
    }

    /// <summary>
    /// Returns a range that accepts exactly the versions rejected by this range.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static VersionRange Complement(VersionRange range)
    {
        System.ArgumentNullException.ThrowIfNull(range);

        if (range.IsEmpty())
        {
            return VersionRange.CreateUniversal();
        }

        VersionRange complement = VersionRange.CreateUniversal();
        int index = 0;

        while (index < range.GetSetCount())
        {
            complement = Intersect(complement, ComplementSet(range.GetSet(index)));
            index = index + 1;
        }

        return complement;
    }

    /// <summary>
    /// Reports whether subset.
    /// </summary>
    /// <param name="left">The left used by the operation.</param>
    /// <param name="right">The right used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static bool IsSubset(VersionRange left, VersionRange right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        return IsUnsatisfiable(Difference(left, right));
    }

    /// <summary>
    /// Reports whether superset.
    /// </summary>
    /// <param name="left">The left used by the operation.</param>
    /// <param name="right">The right used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static bool IsSuperset(VersionRange left, VersionRange right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        return IsSubset(right, left);
    }

    /// <summary>
    /// Reports whether disjoint.
    /// </summary>
    /// <param name="left">The left used by the operation.</param>
    /// <param name="right">The right used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static bool IsDisjoint(VersionRange left, VersionRange right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        return IsUnsatisfiable(Intersect(left, right));
    }

    /// <summary>
    /// Reports whether unsatisfiable.
    /// </summary>
    /// <param name="range">The range used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static bool IsUnsatisfiable(VersionRange range)
    {
        System.ArgumentNullException.ThrowIfNull(range);

        if (range.IsEmpty())
        {
            return true;
        }

        int index = 0;
        while (index < range.GetSetCount())
        {
            if (!IsUnsatisfiable(range.GetSet(index)))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    /// <summary>
    /// Reports whether unsatisfiable.
    /// </summary>
    /// <param name="set">The set used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public static bool IsUnsatisfiable(VersionComparatorSet set)
    {
        System.ArgumentNullException.ThrowIfNull(set);

        UniversalVersion? equalVersion = null;
        VersionRangeAlgebraBounds bounds = new VersionRangeAlgebraBounds();
        System.Collections.Generic.List<UniversalVersion> excludedVersions = new System.Collections.Generic.List<UniversalVersion>();
        int index = 0;

        while (index < set.GetComparatorCount())
        {
            VersionComparator comparator = set.GetComparator(index);
            RangeOperator rangeOperator = comparator.GetOperator();

            if (rangeOperator == RangeOperator.Equal)
            {
                if (equalVersion != null && CompareVersions(equalVersion, comparator.GetVersion(), comparator.GetComparisonMode()) != 0)
                {
                    return true;
                }

                equalVersion = comparator.GetVersion();
            }
            else if (rangeOperator == RangeOperator.NotEqual)
            {
                excludedVersions.Add(comparator.GetVersion());
            }
            else if (rangeOperator == RangeOperator.GreaterThan || rangeOperator == RangeOperator.GreaterThanOrEqual)
            {
                UpdateLowerBound(comparator, bounds);
            }
            else
            {
                UpdateUpperBound(comparator, bounds);
            }

            index = index + 1;
        }

        if (equalVersion != null)
        {
            return !set.ContainsVersion(equalVersion);
        }

        if (BoundsAreContradictory(bounds))
        {
            return true;
        }

        if (BoundsRepresentSingleVersion(bounds))
        {
            return bounds.lowerVersion != null && ExcludesVersion(excludedVersions, bounds.lowerVersion);
        }

        return false;
    }

    internal static VersionRange ComplementSet(VersionComparatorSet set)
    {
        if (set.IsUniversal())
        {
            return VersionRange.CreateEmpty();
        }

        System.Collections.Generic.List<VersionComparatorSet> sets = new System.Collections.Generic.List<VersionComparatorSet>();
        int index = 0;

        while (index < set.GetComparatorCount())
        {
            sets.Add(VersionComparatorSet.CreateSingle(ComplementComparator(set.GetComparator(index))));
            index = index + 1;
        }

        return new VersionRange(sets.ToArray());
    }

    internal static VersionComparator ComplementComparator(VersionComparator comparator)
    {
        RangeOperator rangeOperator = comparator.GetOperator();
        UniversalVersion version = comparator.GetVersion();
        VersionComparisonMode comparisonMode = comparator.GetComparisonMode();

        if (rangeOperator == RangeOperator.Equal)
        {
            return VersionComparator.CreateNotEqual(version, comparisonMode);
        }

        if (rangeOperator == RangeOperator.NotEqual)
        {
            return VersionComparator.CreateEqual(version, comparisonMode);
        }

        if (rangeOperator == RangeOperator.GreaterThan)
        {
            return VersionComparator.CreateLessThanOrEqual(version, comparisonMode);
        }

        if (rangeOperator == RangeOperator.GreaterThanOrEqual)
        {
            return VersionComparator.CreateLessThan(version, comparisonMode);
        }

        if (rangeOperator == RangeOperator.LessThan)
        {
            return VersionComparator.CreateGreaterThanOrEqual(version, comparisonMode);
        }

        return VersionComparator.CreateGreaterThan(version, comparisonMode);
    }

    internal static void UpdateLowerBound(VersionComparator comparator, VersionRangeAlgebraBounds bounds)
    {
        bool comparatorInclusive = comparator.GetOperator() == RangeOperator.GreaterThanOrEqual;

        if (bounds.lowerVersion == null)
        {
            bounds.lowerVersion = comparator.GetVersion();
            bounds.lowerInclusive = comparatorInclusive;
            bounds.lowerComparator = comparator;
            return;
        }

        int comparison = CompareVersions(comparator.GetVersion(), bounds.lowerVersion, comparator.GetComparisonMode());
        if (comparison > 0 || comparison == 0 && bounds.lowerInclusive && !comparatorInclusive)
        {
            bounds.lowerVersion = comparator.GetVersion();
            bounds.lowerInclusive = comparatorInclusive;
            bounds.lowerComparator = comparator;
        }
    }

    internal static void UpdateUpperBound(VersionComparator comparator, VersionRangeAlgebraBounds bounds)
    {
        bool comparatorInclusive = comparator.GetOperator() == RangeOperator.LessThanOrEqual;

        if (bounds.upperVersion == null)
        {
            bounds.upperVersion = comparator.GetVersion();
            bounds.upperInclusive = comparatorInclusive;
            bounds.upperComparator = comparator;
            return;
        }

        int comparison = CompareVersions(comparator.GetVersion(), bounds.upperVersion, comparator.GetComparisonMode());
        if (comparison < 0 || comparison == 0 && bounds.upperInclusive && !comparatorInclusive)
        {
            bounds.upperVersion = comparator.GetVersion();
            bounds.upperInclusive = comparatorInclusive;
            bounds.upperComparator = comparator;
        }
    }

    internal static VersionRangeAlgebraBounds GetBounds(VersionComparatorSet set)
    {
        VersionRangeAlgebraBounds bounds = new VersionRangeAlgebraBounds();
        int index = 0;

        while (index < set.GetComparatorCount())
        {
            VersionComparator comparator = set.GetComparator(index);
            if (comparator.GetOperator() == RangeOperator.GreaterThan || comparator.GetOperator() == RangeOperator.GreaterThanOrEqual)
            {
                UpdateLowerBound(comparator, bounds);
            }
            else if (comparator.GetOperator() == RangeOperator.LessThan || comparator.GetOperator() == RangeOperator.LessThanOrEqual)
            {
                UpdateUpperBound(comparator, bounds);
            }

            index = index + 1;
        }

        return bounds;
    }

    internal static VersionComparator? GetEqualComparator(VersionComparatorSet set)
    {
        int index = 0;

        while (index < set.GetComparatorCount())
        {
            if (set.GetComparator(index).GetOperator() == RangeOperator.Equal)
            {
                return set.GetComparator(index);
            }

            index = index + 1;
        }

        return null;
    }

    internal static void AddRelevantNotEqualComparators(
        VersionComparatorSet set,
        VersionRangeAlgebraBounds bounds,
        System.Collections.Generic.List<VersionComparator> comparators)
    {
        int index = 0;

        while (index < set.GetComparatorCount())
        {
            VersionComparator comparator = set.GetComparator(index);
            if (comparator.GetOperator() == RangeOperator.NotEqual
                && IsVersionAllowedByBounds(comparator.GetVersion(), bounds)
                && !ContainsEquivalentNotEqual(comparators, comparator))
            {
                comparators.Add(comparator);
            }

            index = index + 1;
        }
    }

    internal static bool IsVersionAllowedByBounds(UniversalVersion version, VersionRangeAlgebraBounds bounds)
    {
        if (bounds.lowerVersion != null)
        {
            int lowerComparison = version.CompareToVersion(bounds.lowerVersion, VersionComparisonMode.Precedence);
            if (lowerComparison < 0 || lowerComparison == 0 && !bounds.lowerInclusive)
            {
                return false;
            }
        }

        if (bounds.upperVersion != null)
        {
            int upperComparison = version.CompareToVersion(bounds.upperVersion, VersionComparisonMode.Precedence);
            if (upperComparison > 0 || upperComparison == 0 && !bounds.upperInclusive)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool ContainsEquivalentNotEqual(
        System.Collections.Generic.List<VersionComparator> comparators,
        VersionComparator comparator)
    {
        int index = 0;

        while (index < comparators.Count)
        {
            if (comparators[index].GetOperator() == RangeOperator.NotEqual
                && CompareVersions(comparators[index].GetVersion(), comparator.GetVersion(), comparator.GetComparisonMode()) == 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsEquivalentCandidate(
        System.Collections.Generic.List<UniversalVersion> candidates,
        UniversalVersion candidate)
    {
        int index = 0;

        while (index < candidates.Count)
        {
            if (CompareVersions(candidates[index], candidate, VersionComparisonMode.Precedence) == 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool BoundsAreContradictory(VersionRangeAlgebraBounds bounds)
    {
        if (bounds.lowerVersion == null || bounds.upperVersion == null)
        {
            return false;
        }

        int comparison = bounds.lowerVersion.CompareToVersion(bounds.upperVersion, VersionComparisonMode.Precedence);
        if (comparison > 0)
        {
            return true;
        }

        if (comparison == 0 && (!bounds.lowerInclusive || !bounds.upperInclusive))
        {
            return true;
        }

        return false;
    }

    internal static bool BoundsRepresentSingleVersion(VersionRangeAlgebraBounds bounds)
    {
        if (bounds.lowerVersion == null || bounds.upperVersion == null)
        {
            return false;
        }

        return bounds.lowerInclusive
            && bounds.upperInclusive
            && bounds.lowerVersion.CompareToVersion(bounds.upperVersion, VersionComparisonMode.Precedence) == 0;
    }

    internal static bool ExcludesVersion(System.Collections.Generic.List<UniversalVersion> excludedVersions, UniversalVersion version)
    {
        if (version == null)
        {
            return false;
        }

        int index = 0;
        while (index < excludedVersions.Count)
        {
            if (CompareVersions(version, excludedVersions[index], VersionComparisonMode.Precedence) == 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int CompareVersions(UniversalVersion left, UniversalVersion right, VersionComparisonMode comparisonMode)
    {
        return left.CompareToVersion(right, comparisonMode);
    }

    internal static UniversalVersion ParseRequiredVersion(VersionParseResult result)
    {
        if (result.IsFailure())
        {
            throw new System.InvalidOperationException("Internal minimum version parsing failed.");
        }

        return result.GetVersion();
    }
}

internal class VersionRangeAlgebraBounds
{
    internal UniversalVersion? lowerVersion;
    internal bool lowerInclusive;
    internal VersionComparator? lowerComparator;
    internal UniversalVersion? upperVersion;
    internal bool upperInclusive;
    internal VersionComparator? upperComparator;

    internal VersionRangeAlgebraBounds()
    {
        lowerVersion = null;
        lowerInclusive = true;
        lowerComparator = null;
        upperVersion = null;
        upperInclusive = true;
        upperComparator = null;
    }
}
