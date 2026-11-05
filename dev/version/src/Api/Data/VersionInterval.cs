namespace version;

/// <summary>
/// Represents a version interval.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class VersionInterval
{
    /// <summary>
    /// Stores the <c>lowerKind</c> value.
    /// </summary>
    public BoundKind lowerKind;
    /// <summary>
    /// Stores the <c>lowerVersion</c> value.
    /// </summary>
    public UniversalVersion? lowerVersion;
    /// <summary>
    /// Stores the <c>upperKind</c> value.
    /// </summary>
    public BoundKind upperKind;
    /// <summary>
    /// Stores the <c>upperVersion</c> value.
    /// </summary>
    public UniversalVersion? upperVersion;
    /// <summary>
    /// Stores the <c>comparisonMode</c> value.
    /// </summary>
    public VersionComparisonMode comparisonMode;

    /// <summary>
    /// Initializes a new Version Interval instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="lowerKindValue">The lower kind value used by the operation.</param>
    /// <param name="lowerVersionValue">The lower version value used by the operation.</param>
    /// <param name="upperKindValue">The upper kind value used by the operation.</param>
    /// <param name="upperVersionValue">The upper version value used by the operation.</param>
    /// <param name="comparisonModeValue">The comparison mode value used by the operation.</param>
    public VersionInterval(
        BoundKind lowerKindValue,
        UniversalVersion? lowerVersionValue,
        BoundKind upperKindValue,
        UniversalVersion? upperVersionValue,
        VersionComparisonMode comparisonModeValue)
    {
        ValidateBound(lowerKindValue, lowerVersionValue, "lowerVersionValue");
        ValidateBound(upperKindValue, upperVersionValue, "upperVersionValue");
        ValidateBoundOrder(lowerKindValue, lowerVersionValue, upperKindValue, upperVersionValue, comparisonModeValue);

        lowerKind = lowerKindValue;
        lowerVersion = lowerVersionValue;
        upperKind = upperKindValue;
        upperVersion = upperVersionValue;
        comparisonMode = comparisonModeValue;
    }

    /// <summary>
    /// Creates an unbounded.
    /// </summary>
    /// <param name="comparisonMode">The comparison mode used by the operation.</param>
    /// <returns>The resulting version interval.</returns>
    internal static VersionInterval CreateUnbounded(VersionComparisonMode comparisonMode)
    {
        return new VersionInterval(BoundKind.Unbounded, null, BoundKind.Unbounded, null, comparisonMode);
    }

    /// <summary>
    /// Creates a lower bound.
    /// </summary>
    /// <param name="lowerKind">The lower kind used by the operation.</param>
    /// <param name="lowerVersion">The lower version used by the operation.</param>
    /// <param name="comparisonMode">The comparison mode used by the operation.</param>
    /// <returns>The resulting version interval.</returns>
    internal static VersionInterval CreateLowerBound(BoundKind lowerKind, UniversalVersion lowerVersion, VersionComparisonMode comparisonMode)
    {
        return new VersionInterval(lowerKind, lowerVersion, BoundKind.Unbounded, null, comparisonMode);
    }

    /// <summary>
    /// Creates an upper bound.
    /// </summary>
    /// <param name="upperKind">The upper kind used by the operation.</param>
    /// <param name="upperVersion">The upper version used by the operation.</param>
    /// <param name="comparisonMode">The comparison mode used by the operation.</param>
    /// <returns>The resulting version interval.</returns>
    internal static VersionInterval CreateUpperBound(BoundKind upperKind, UniversalVersion upperVersion, VersionComparisonMode comparisonMode)
    {
        return new VersionInterval(BoundKind.Unbounded, null, upperKind, upperVersion, comparisonMode);
    }

    /// <summary>
    /// Creates a bounded.
    /// </summary>
    /// <param name="lowerKind">The lower kind used by the operation.</param>
    /// <param name="lowerVersion">The lower version used by the operation.</param>
    /// <param name="upperKind">The upper kind used by the operation.</param>
    /// <param name="upperVersion">The upper version used by the operation.</param>
    /// <param name="comparisonMode">The comparison mode used by the operation.</param>
    /// <returns>The resulting version interval.</returns>
    internal static VersionInterval CreateBounded(
        BoundKind lowerKind,
        UniversalVersion lowerVersion,
        BoundKind upperKind,
        UniversalVersion upperVersion,
        VersionComparisonMode comparisonMode)
    {
        return new VersionInterval(lowerKind, lowerVersion, upperKind, upperVersion, comparisonMode);
    }

    /// <summary>
    /// Gets a lower kind.
    /// </summary>
    /// <returns>The resulting bound kind.</returns>
    internal BoundKind GetLowerKind()
    {
        return lowerKind;
    }

    /// <summary>
    /// Gets an upper kind.
    /// </summary>
    /// <returns>The resulting bound kind.</returns>
    internal BoundKind GetUpperKind()
    {
        return upperKind;
    }

    /// <summary>
    /// Reports whether this value has lower bound.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasLowerBound()
    {
        return lowerKind != BoundKind.Unbounded;
    }

    /// <summary>
    /// Reports whether this value has upper bound.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasUpperBound()
    {
        return upperKind != BoundKind.Unbounded;
    }

    /// <summary>
    /// Gets a lower version.
    /// </summary>
    /// <returns>The resulting universal version.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal UniversalVersion GetLowerVersion()
    {
        if (lowerVersion == null)
        {
            throw new System.InvalidOperationException("Interval does not have a lower bound.");
        }

        return lowerVersion;
    }

    /// <summary>
    /// Gets an upper version.
    /// </summary>
    /// <returns>The resulting universal version.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal UniversalVersion GetUpperVersion()
    {
        if (upperVersion == null)
        {
            throw new System.InvalidOperationException("Interval does not have an upper bound.");
        }

        return upperVersion;
    }

    /// <summary>
    /// Gets a comparison mode.
    /// </summary>
    /// <returns>The resulting version comparison mode.</returns>
    internal VersionComparisonMode GetComparisonMode()
    {
        return comparisonMode;
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

        if (!ContainsLowerBound(version))
        {
            return false;
        }

        return ContainsUpperBound(version);
    }

    /// <summary>
    /// Formats this value as comparator set.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <returns>The resulting version comparator set.</returns>
    internal VersionComparatorSet ToComparatorSet()
    {
        int count = GetComparatorCount();
        VersionComparator[] comparators = new VersionComparator[count];
        int index = 0;

        if (HasLowerBound())
        {
            comparators[index] = CreateLowerComparator();
            index = index + 1;
        }

        if (HasUpperBound())
        {
            comparators[index] = CreateUpperComparator();
        }

        return new VersionComparatorSet(comparators);
    }

    /// <summary>
    /// Checks whether this value contains version except.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="excludedVersion">The excluded version used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool ContainsVersionExcept(UniversalVersion version, UniversalVersion excludedVersion)
    {
        System.ArgumentNullException.ThrowIfNull(version);
        System.ArgumentNullException.ThrowIfNull(excludedVersion);

        if (!ContainsVersion(version))
        {
            return false;
        }

        return version.CompareToVersion(excludedVersion, comparisonMode) != 0;
    }

    /// <summary>
    /// Checks whether this value contains exclusion hole.
    /// </summary>
    /// <param name="excludedVersion">The excluded version used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool ContainsExclusionHole(UniversalVersion excludedVersion)
    {
        System.ArgumentNullException.ThrowIfNull(excludedVersion);

        return ContainsVersion(excludedVersion);
    }

    /// <summary>
    /// Formats this value as comparator set excluding.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="excludedVersion">The excluded version used by the operation.</param>
    /// <returns>The resulting version comparator set.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal VersionComparatorSet ToComparatorSetExcluding(UniversalVersion excludedVersion)
    {
        System.ArgumentNullException.ThrowIfNull(excludedVersion);

        VersionComparatorSet intervalSet = ToComparatorSet();
        VersionComparatorSet exclusionSet = VersionComparatorSet.CreateSingle(
            VersionComparator.CreateNotEqual(excludedVersion, comparisonMode));

        return intervalSet.Intersect(exclusionSet);
    }

    /// <summary>
    /// Formats this value as range excluding.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="excludedVersion">The excluded version used by the operation.</param>
    /// <returns>The resulting version range.</returns>
    internal VersionRange ToRangeExcluding(UniversalVersion excludedVersion)
    {
        return VersionRange.CreateSingleSet(ToComparatorSetExcluding(excludedVersion));
    }

    internal bool ContainsLowerBound(UniversalVersion version)
    {
        if (!HasLowerBound())
        {
            return true;
        }

        int comparison = version.CompareToVersion(GetLowerVersion(), comparisonMode);
        if (lowerKind == BoundKind.Inclusive)
        {
            return comparison >= 0;
        }

        return comparison > 0;
    }

    internal bool ContainsUpperBound(UniversalVersion version)
    {
        if (!HasUpperBound())
        {
            return true;
        }

        int comparison = version.CompareToVersion(GetUpperVersion(), comparisonMode);
        if (upperKind == BoundKind.Inclusive)
        {
            return comparison <= 0;
        }

        return comparison < 0;
    }

    internal int GetComparatorCount()
    {
        int count = 0;

        if (HasLowerBound())
        {
            count = count + 1;
        }

        if (HasUpperBound())
        {
            count = count + 1;
        }

        return count;
    }

    internal VersionComparator CreateLowerComparator()
    {
        if (lowerKind == BoundKind.Inclusive)
        {
            return VersionComparator.CreateGreaterThanOrEqual(GetLowerVersion(), comparisonMode);
        }

        return VersionComparator.CreateGreaterThan(GetLowerVersion(), comparisonMode);
    }

    internal VersionComparator CreateUpperComparator()
    {
        if (upperKind == BoundKind.Inclusive)
        {
            return VersionComparator.CreateLessThanOrEqual(GetUpperVersion(), comparisonMode);
        }

        return VersionComparator.CreateLessThan(GetUpperVersion(), comparisonMode);
    }

    internal static void ValidateBound(BoundKind kind, UniversalVersion? version, string name)
    {
        ValidateBoundKind(kind);

        if (kind == BoundKind.Unbounded)
        {
            if (version != null)
            {
                throw new System.ArgumentException(name + " must be null when the bound is unbounded.");
            }

            return;
        }

        if (version == null)
        {
            throw new System.ArgumentNullException(name);
        }
    }

    internal static void ValidateBoundOrder(
        BoundKind lowerKind,
        UniversalVersion? lowerVersion,
        BoundKind upperKind,
        UniversalVersion? upperVersion,
        VersionComparisonMode comparisonMode)
    {
        if (lowerKind == BoundKind.Unbounded || upperKind == BoundKind.Unbounded)
        {
            return;
        }

        if (lowerVersion == null || upperVersion == null)
        {
            return;
        }

        int comparison = lowerVersion.CompareToVersion(upperVersion, comparisonMode);
        if (comparison > 0)
        {
            throw new System.ArgumentException("Interval lower bound cannot be greater than upper bound.");
        }

        if (comparison == 0 && (lowerKind == BoundKind.Exclusive || upperKind == BoundKind.Exclusive))
        {
            throw new System.ArgumentException("Equal interval bounds must both be inclusive.");
        }
    }

    internal static void ValidateBoundKind(BoundKind kind)
    {
        if (kind == BoundKind.Unbounded)
        {
            return;
        }

        if (kind == BoundKind.Inclusive)
        {
            return;
        }

        if (kind == BoundKind.Exclusive)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("kind");
    }
}
