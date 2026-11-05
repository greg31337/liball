namespace version;

/// <summary>
/// Represents the optional minimum version admitted by a version range.
/// </summary>
/// <remarks>
/// A result without a value distinguishes an empty range or an unrepresentable infimum from a represented minimum version.
/// </remarks>
public class VersionRangeMinimumResult
{
    /// <summary>
    /// Stores the <c>hasValue</c> value.
    /// </summary>
    public bool hasValue;
    /// <summary>
    /// Stores the <c>version</c> value.
    /// </summary>
    public UniversalVersion? version;

    /// <summary>
    /// Initializes a new Version Range Minimum Result instance from the supplied values.
    /// </summary>
    /// <param name="hasValueValue">Whether the result contains a represented minimum.</param>
    /// <param name="versionValue">The represented minimum version, or null when no value exists.</param>
    /// <exception cref="System.ArgumentException">The value flag and supplied version are inconsistent.</exception>
    public VersionRangeMinimumResult(bool hasValueValue, UniversalVersion? versionValue)
    {
        if (hasValueValue && versionValue == null)
        {
            throw new System.ArgumentException("A minimum result with a value must contain a version.");
        }

        if (!hasValueValue && versionValue != null)
        {
            throw new System.ArgumentException("A minimum result without a value cannot contain a version.");
        }

        hasValue = hasValueValue;
        version = versionValue;
    }

    internal static VersionRangeMinimumResult CreateValue(UniversalVersion versionValue)
    {
        System.ArgumentNullException.ThrowIfNull(versionValue);
        return new VersionRangeMinimumResult(true, versionValue);
    }

    internal static VersionRangeMinimumResult CreateNoValue()
    {
        return new VersionRangeMinimumResult(false, null);
    }
}
