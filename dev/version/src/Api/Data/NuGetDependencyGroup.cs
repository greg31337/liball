namespace version;

/// <summary>
/// Represents a nuget dependency group.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetDependencyGroup
{
    /// <summary>
    /// Stores the <c>targetFramework</c> value.
    /// </summary>
    public string targetFramework;
    /// <summary>
    /// Stores the <c>targetFrameworkSpecified</c> value.
    /// </summary>
    public bool targetFrameworkSpecified;
    /// <summary>
    /// Stores the <c>dependencies</c> value.
    /// </summary>
    public NuGetDependency[] dependencies;

    /// <summary>
    /// Initializes a new NuGet Dependency Group instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="targetFrameworkValue">The target framework value used by the operation.</param>
    /// <param name="dependencyValues">The dependency values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public NuGetDependencyGroup(string? targetFrameworkValue, NuGetDependency[] dependencyValues)
    {
        System.ArgumentNullException.ThrowIfNull(dependencyValues);

        if (targetFrameworkValue == null)
        {
            targetFramework = "";
            targetFrameworkSpecified = false;
        }
        else
        {
            if (targetFrameworkValue.Length == 0)
            {
                targetFramework = "";
                targetFrameworkSpecified = false;
            }
            else
            {
                targetFramework = targetFrameworkValue;
                targetFrameworkSpecified = true;
            }
        }

        dependencies = CopyDependencies(dependencyValues);
    }

    /// <summary>
    /// Creates an any framework.
    /// </summary>
    /// <param name="dependencies">The dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting nuget dependency group.</returns>
    internal static NuGetDependencyGroup CreateAnyFramework(NuGetDependency[] dependencies)
    {
        return new NuGetDependencyGroup(null, dependencies);
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="targetFramework">The target framework used by the operation.</param>
    /// <param name="dependencies">The dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting nuget dependency group.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static NuGetDependencyGroup Create(string targetFramework, NuGetDependency[] dependencies)
    {
        System.ArgumentNullException.ThrowIfNull(targetFramework);

        return new NuGetDependencyGroup(targetFramework, dependencies);
    }

    /// <summary>
    /// Reports whether this value has target framework.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasTargetFramework()
    {
        return targetFrameworkSpecified;
    }

    /// <summary>
    /// Gets a target framework.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetTargetFramework()
    {
        return targetFramework;
    }

    /// <summary>
    /// Gets a dependency count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetDependencyCount()
    {
        return dependencies.Length;
    }

    /// <summary>
    /// Reports whether this range or collection accepts no values.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsEmpty()
    {
        return dependencies.Length == 0;
    }

    /// <summary>
    /// Gets a dependency.
    /// </summary>
    /// <param name="dependencyIndex">The zero-based dependency index.</param>
    /// <returns>The resulting nuget dependency.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal NuGetDependency GetDependency(int dependencyIndex)
    {
        if (dependencyIndex < 0 || dependencyIndex >= dependencies.Length)
        {
            throw new System.ArgumentOutOfRangeException("dependencyIndex");
        }

        return dependencies[dependencyIndex];
    }

    /// <summary>
    /// Gets a dependencies.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal NuGetDependency[] GetDependencies()
    {
        return CopyDependencies(dependencies);
    }

    internal static NuGetDependency[] CopyDependencies(NuGetDependency[] dependencyValues)
    {
        NuGetDependency[] copy = new NuGetDependency[dependencyValues.Length];
        int index = 0;

        while (index < dependencyValues.Length)
        {
            if (dependencyValues[index] == null)
            {
                throw new System.ArgumentException("NuGet dependency arrays cannot contain null values.");
            }

            copy[index] = dependencyValues[index];
            index = index + 1;
        }

        return copy;
    }
}
