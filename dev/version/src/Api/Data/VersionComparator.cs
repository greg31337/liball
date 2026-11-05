namespace version;

/// <summary>
/// Represents one comparison between a candidate version and a fixed version.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class VersionComparator
{
    /// <summary>
    /// Stores the <c>rangeOperator</c> value.
    /// </summary>
    public RangeOperator rangeOperator;
    /// <summary>
    /// Stores the <c>version</c> value.
    /// </summary>
    public UniversalVersion version;
    /// <summary>
    /// Stores the <c>comparisonMode</c> value.
    /// </summary>
    public VersionComparisonMode comparisonMode;

    /// <summary>
    /// Initializes a new Version Comparator instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="operatorValue">The operator value used by the operation.</param>
    /// <param name="versionValue">The version value used by the operation.</param>
    /// <param name="comparisonModeValue">The comparison mode value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public VersionComparator(RangeOperator operatorValue, UniversalVersion versionValue, VersionComparisonMode comparisonModeValue)
    {
        System.ArgumentNullException.ThrowIfNull(versionValue);
        ValidateOperator(operatorValue);

        rangeOperator = operatorValue;
        version = versionValue;
        comparisonMode = comparisonModeValue;
    }

    /// <summary>
    /// Creates an equal.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="comparisonMode">The comparison mode used by the operation.</param>
    /// <returns>The resulting version comparator.</returns>
    internal static VersionComparator CreateEqual(UniversalVersion version, VersionComparisonMode comparisonMode)
    {
        return new VersionComparator(RangeOperator.Equal, version, comparisonMode);
    }

    /// <summary>
    /// Creates a not equal.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="comparisonMode">The comparison mode used by the operation.</param>
    /// <returns>The resulting version comparator.</returns>
    internal static VersionComparator CreateNotEqual(UniversalVersion version, VersionComparisonMode comparisonMode)
    {
        return new VersionComparator(RangeOperator.NotEqual, version, comparisonMode);
    }

    /// <summary>
    /// Creates a greater than.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="comparisonMode">The comparison mode used by the operation.</param>
    /// <returns>The resulting version comparator.</returns>
    internal static VersionComparator CreateGreaterThan(UniversalVersion version, VersionComparisonMode comparisonMode)
    {
        return new VersionComparator(RangeOperator.GreaterThan, version, comparisonMode);
    }

    /// <summary>
    /// Creates a greater than or equal.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="comparisonMode">The comparison mode used by the operation.</param>
    /// <returns>The resulting version comparator.</returns>
    internal static VersionComparator CreateGreaterThanOrEqual(UniversalVersion version, VersionComparisonMode comparisonMode)
    {
        return new VersionComparator(RangeOperator.GreaterThanOrEqual, version, comparisonMode);
    }

    /// <summary>
    /// Creates a less than.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="comparisonMode">The comparison mode used by the operation.</param>
    /// <returns>The resulting version comparator.</returns>
    internal static VersionComparator CreateLessThan(UniversalVersion version, VersionComparisonMode comparisonMode)
    {
        return new VersionComparator(RangeOperator.LessThan, version, comparisonMode);
    }

    /// <summary>
    /// Creates a less than or equal.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="comparisonMode">The comparison mode used by the operation.</param>
    /// <returns>The resulting version comparator.</returns>
    internal static VersionComparator CreateLessThanOrEqual(UniversalVersion version, VersionComparisonMode comparisonMode)
    {
        return new VersionComparator(RangeOperator.LessThanOrEqual, version, comparisonMode);
    }

    /// <summary>
    /// Gets an operator.
    /// </summary>
    /// <returns>The resulting range operator.</returns>
    internal RangeOperator GetOperator()
    {
        return rangeOperator;
    }

    /// <summary>
    /// Gets a version.
    /// </summary>
    /// <returns>The resulting universal version.</returns>
    internal UniversalVersion GetVersion()
    {
        return version;
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
    /// <param name="candidate">The candidate used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool ContainsVersion(UniversalVersion candidate)
    {
        System.ArgumentNullException.ThrowIfNull(candidate);

        return ContainsRelation(GetRelation(candidate));
    }

    /// <summary>
    /// Checks whether this value contains relation.
    /// </summary>
    /// <param name="relation">The relation used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool ContainsRelation(VersionRelation relation)
    {
        if (rangeOperator == RangeOperator.Equal)
        {
            return relation == VersionRelation.EqualTo;
        }

        if (rangeOperator == RangeOperator.NotEqual)
        {
            return relation != VersionRelation.EqualTo;
        }

        if (rangeOperator == RangeOperator.GreaterThan)
        {
            return relation == VersionRelation.GreaterThan;
        }

        if (rangeOperator == RangeOperator.GreaterThanOrEqual)
        {
            return relation == VersionRelation.GreaterThan || relation == VersionRelation.EqualTo;
        }

        if (rangeOperator == RangeOperator.LessThan)
        {
            return relation == VersionRelation.LessThan;
        }

        return relation == VersionRelation.LessThan || relation == VersionRelation.EqualTo;
    }

    /// <summary>
    /// Gets an operator text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetOperatorText()
    {
        if (rangeOperator == RangeOperator.Equal)
        {
            return "=";
        }

        if (rangeOperator == RangeOperator.NotEqual)
        {
            return "!=";
        }

        if (rangeOperator == RangeOperator.GreaterThan)
        {
            return ">";
        }

        if (rangeOperator == RangeOperator.GreaterThanOrEqual)
        {
            return ">=";
        }

        if (rangeOperator == RangeOperator.LessThan)
        {
            return "<";
        }

        return "<=";
    }

    internal VersionRelation GetRelation(UniversalVersion candidate)
    {
        int comparison = candidate.CompareToVersion(version, comparisonMode);

        if (comparison < 0)
        {
            return VersionRelation.LessThan;
        }

        if (comparison > 0)
        {
            return VersionRelation.GreaterThan;
        }

        return VersionRelation.EqualTo;
    }

    internal static void ValidateOperator(RangeOperator operatorValue)
    {
        if (operatorValue == RangeOperator.Equal)
        {
            return;
        }

        if (operatorValue == RangeOperator.NotEqual)
        {
            return;
        }

        if (operatorValue == RangeOperator.GreaterThan)
        {
            return;
        }

        if (operatorValue == RangeOperator.GreaterThanOrEqual)
        {
            return;
        }

        if (operatorValue == RangeOperator.LessThan)
        {
            return;
        }

        if (operatorValue == RangeOperator.LessThanOrEqual)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("operatorValue");
    }
}
