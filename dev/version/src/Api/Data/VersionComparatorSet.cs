namespace version;

/// <summary>
/// Represents comparators that must all match the same candidate version.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class VersionComparatorSet
{
    /// <summary>
    /// Stores the <c>comparators</c> value.
    /// </summary>
    public VersionComparator[] comparators;

    /// <summary>
    /// Initializes a new Version Comparator Set instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="comparatorValues">The comparator values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public VersionComparatorSet(VersionComparator[] comparatorValues)
    {
        System.ArgumentNullException.ThrowIfNull(comparatorValues);

        comparators = CopyComparators(comparatorValues);
    }

    /// <summary>
    /// Creates an universal.
    /// </summary>
    /// <returns>The resulting version comparator set.</returns>
    internal static VersionComparatorSet CreateUniversal()
    {
        return new VersionComparatorSet(new VersionComparator[0]);
    }

    /// <summary>
    /// Creates a single.
    /// </summary>
    /// <param name="comparator">The comparator used by the operation.</param>
    /// <returns>The resulting version comparator set.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static VersionComparatorSet CreateSingle(VersionComparator comparator)
    {
        System.ArgumentNullException.ThrowIfNull(comparator);

        VersionComparator[] comparators = new VersionComparator[1];
        comparators[0] = comparator;
        return new VersionComparatorSet(comparators);
    }

    /// <summary>
    /// Returns the intersection that accepts only values admitted by both inputs.
    /// </summary>
    /// <param name="left">The left used by the operation.</param>
    /// <param name="right">The right used by the operation.</param>
    /// <returns>The resulting version comparator set.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static VersionComparatorSet Intersect(VersionComparatorSet left, VersionComparatorSet right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        return left.Intersect(right);
    }

    /// <summary>
    /// Gets a comparator count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetComparatorCount()
    {
        return comparators.Length;
    }

    /// <summary>
    /// Gets a comparator.
    /// </summary>
    /// <param name="comparatorIndex">The zero-based comparator index.</param>
    /// <returns>The resulting version comparator.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal VersionComparator GetComparator(int comparatorIndex)
    {
        if (comparatorIndex < 0 || comparatorIndex >= comparators.Length)
        {
            throw new System.ArgumentOutOfRangeException("comparatorIndex");
        }

        return comparators[comparatorIndex];
    }

    /// <summary>
    /// Gets a comparators.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal VersionComparator[] GetComparators()
    {
        return CopyComparators(comparators);
    }

    /// <summary>
    /// Reports whether universal.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsUniversal()
    {
        return comparators.Length == 0;
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

        int index = 0;
        while (index < comparators.Length)
        {
            if (!comparators[index].ContainsVersion(version))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    /// <summary>
    /// Returns the intersection that accepts only values admitted by both inputs.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns>The resulting version comparator set.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal VersionComparatorSet Intersect(VersionComparatorSet other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        VersionComparator[] combined = new VersionComparator[comparators.Length + other.GetComparatorCount()];
        int writeIndex = 0;
        int readIndex = 0;

        while (readIndex < comparators.Length)
        {
            combined[writeIndex] = comparators[readIndex];
            writeIndex = writeIndex + 1;
            readIndex = readIndex + 1;
        }

        readIndex = 0;
        while (readIndex < other.GetComparatorCount())
        {
            combined[writeIndex] = other.GetComparator(readIndex);
            writeIndex = writeIndex + 1;
            readIndex = readIndex + 1;
        }

        return new VersionComparatorSet(combined);
    }

    internal static VersionComparator[] CopyComparators(VersionComparator[] comparatorValues)
    {
        VersionComparator[] copy = new VersionComparator[comparatorValues.Length];
        int index = 0;

        while (index < comparatorValues.Length)
        {
            if (comparatorValues[index] == null)
            {
                throw new System.ArgumentException("Comparator sets cannot contain null values.");
            }

            copy[index] = comparatorValues[index];
            index = index + 1;
        }

        return copy;
    }
}
