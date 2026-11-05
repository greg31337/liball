namespace version;

/// <summary>
/// Represents a nuget package source priority.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetPackageSourcePriority
{
    /// <summary>
    /// Stores the <c>source</c> value.
    /// </summary>
    public NuGetSourceIdentity source;
    /// <summary>
    /// Stores the <c>priority</c> value.
    /// </summary>
    public int priority;
    /// <summary>
    /// Stores the <c>enabled</c> value.
    /// </summary>
    public bool enabled;

    /// <summary>
    /// Initializes a new NuGet Package Source Priority instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="sourceValue">The source value used by the operation.</param>
    /// <param name="priorityValue">The priority value used by the operation.</param>
    /// <param name="enabledValue">Whether to enable enabled value.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public NuGetPackageSourcePriority(NuGetSourceIdentity sourceValue, int priorityValue, bool enabledValue)
    {
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        if (priorityValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("priorityValue");
        }

        source = sourceValue;
        priority = priorityValue;
        enabled = enabledValue;
    }

    /// <summary>
    /// Creates an enabled.
    /// </summary>
    /// <param name="source">The source used by the operation.</param>
    /// <param name="priority">The priority used by the operation.</param>
    /// <returns>The resulting nuget package source priority.</returns>
    internal static NuGetPackageSourcePriority CreateEnabled(NuGetSourceIdentity source, int priority)
    {
        return new NuGetPackageSourcePriority(source, priority, true);
    }

    /// <summary>
    /// Creates a disabled.
    /// </summary>
    /// <param name="source">The source used by the operation.</param>
    /// <param name="priority">The priority used by the operation.</param>
    /// <returns>The resulting nuget package source priority.</returns>
    internal static NuGetPackageSourcePriority CreateDisabled(NuGetSourceIdentity source, int priority)
    {
        return new NuGetPackageSourcePriority(source, priority, false);
    }

    /// <summary>
    /// Gets a source.
    /// </summary>
    /// <returns>The resulting nuget source identity.</returns>
    internal NuGetSourceIdentity GetSource()
    {
        return source;
    }

    /// <summary>
    /// Gets a priority.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetPriority()
    {
        return priority;
    }

    /// <summary>
    /// Reports whether enabled.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsEnabled()
    {
        return enabled;
    }

    /// <summary>
    /// Reports whether this package source has higher precedence than the supplied source.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool ComesBefore(NuGetPackageSourcePriority other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        if (enabled && !other.IsEnabled())
        {
            return true;
        }

        if (!enabled && other.IsEnabled())
        {
            return false;
        }

        if (priority < other.GetPriority())
        {
            return true;
        }

        if (priority > other.GetPriority())
        {
            return false;
        }

        return string.Compare(source.GetNormalizedSource(), other.GetSource().GetNormalizedSource(), System.StringComparison.Ordinal) < 0;
    }
}
