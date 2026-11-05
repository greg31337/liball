namespace version;

/// <summary>
/// Represents a nuget target framework fallback list.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetTargetFrameworkFallbackList
{
    /// <summary>
    /// Stores the <c>frameworks</c> value.
    /// </summary>
    public NuGetTargetFramework[] frameworks;

    /// <summary>
    /// Initializes a new NuGet Target Framework Fallback List instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="frameworkValues">The framework values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public NuGetTargetFrameworkFallbackList(NuGetTargetFramework[] frameworkValues)
    {
        System.ArgumentNullException.ThrowIfNull(frameworkValues);

        frameworks = CopyFrameworks(frameworkValues);
    }

    /// <summary>
    /// Gets a framework count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetFrameworkCount()
    {
        return frameworks.Length;
    }

    /// <summary>
    /// Gets a framework.
    /// </summary>
    /// <param name="frameworkIndex">The zero-based framework index.</param>
    /// <returns>The resulting nuget target framework.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal NuGetTargetFramework GetFramework(int frameworkIndex)
    {
        if (frameworkIndex < 0 || frameworkIndex >= frameworks.Length)
        {
            throw new System.ArgumentOutOfRangeException("frameworkIndex");
        }

        return frameworks[frameworkIndex];
    }

    /// <summary>
    /// Gets a frameworks.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal NuGetTargetFramework[] GetFrameworks()
    {
        return CopyFrameworks(frameworks);
    }

    internal static NuGetTargetFramework[] CopyFrameworks(NuGetTargetFramework[] frameworkValues)
    {
        NuGetTargetFramework[] copy = new NuGetTargetFramework[frameworkValues.Length];
        int index = 0;

        while (index < frameworkValues.Length)
        {
            if (frameworkValues[index] == null)
            {
                throw new System.ArgumentException("Target framework fallback arrays cannot contain null values.");
            }

            copy[index] = frameworkValues[index];
            index = index + 1;
        }

        return copy;
    }
}
