namespace version;

/// <summary>
/// Represents the outcome of a cargo feature operation.
/// </summary>
/// <remarks>
/// Check the success state before requesting the produced value. Failure results retain diagnostics that explain why processing stopped.
/// </remarks>
public class CargoFeatureResolution
{
    /// <summary>
    /// Stores the <c>rootFeatures</c> value.
    /// </summary>
    public string[] rootFeatures;
    /// <summary>
    /// Stores the <c>activeDependencies</c> value.
    /// </summary>
    public CargoDependency[] activeDependencies;
    /// <summary>
    /// Stores the <c>dependencyFeatures</c> value.
    /// </summary>
    public CargoResolvedDependencyFeatures[] dependencyFeatures;

    /// <summary>
    /// Initializes a new Cargo Feature Resolution instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="rootFeatureValues">The root feature values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="activeDependencyValues">The active dependency values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="dependencyFeatureValues">The dependency feature values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public CargoFeatureResolution(
        string[] rootFeatureValues,
        CargoDependency[] activeDependencyValues,
        CargoResolvedDependencyFeatures[] dependencyFeatureValues)
    {
        System.ArgumentNullException.ThrowIfNull(rootFeatureValues);
        System.ArgumentNullException.ThrowIfNull(activeDependencyValues);
        System.ArgumentNullException.ThrowIfNull(dependencyFeatureValues);

        rootFeatures = CopyStrings(rootFeatureValues);
        activeDependencies = CopyDependencies(activeDependencyValues);
        dependencyFeatures = CopyDependencyFeatures(dependencyFeatureValues);
    }

    /// <summary>
    /// Gets a root feature count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetRootFeatureCount()
    {
        return rootFeatures.Length;
    }

    /// <summary>
    /// Gets a root feature.
    /// </summary>
    /// <param name="featureIndex">The zero-based feature index.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal string GetRootFeature(int featureIndex)
    {
        if (featureIndex < 0 || featureIndex >= rootFeatures.Length)
        {
            throw new System.ArgumentOutOfRangeException("featureIndex");
        }

        return rootFeatures[featureIndex];
    }

    /// <summary>
    /// Reports whether this value has root feature.
    /// </summary>
    /// <param name="featureName">The feature name used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasRootFeature(string featureName)
    {
        return ContainsText(rootFeatures, featureName);
    }

    /// <summary>
    /// Gets a root features.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal string[] GetRootFeatures()
    {
        return CopyStrings(rootFeatures);
    }

    /// <summary>
    /// Gets an active dependency count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetActiveDependencyCount()
    {
        return activeDependencies.Length;
    }

    /// <summary>
    /// Gets an active dependency.
    /// </summary>
    /// <param name="dependencyIndex">The zero-based dependency index.</param>
    /// <returns>The resulting cargo dependency.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal CargoDependency GetActiveDependency(int dependencyIndex)
    {
        if (dependencyIndex < 0 || dependencyIndex >= activeDependencies.Length)
        {
            throw new System.ArgumentOutOfRangeException("dependencyIndex");
        }

        return activeDependencies[dependencyIndex];
    }

    /// <summary>
    /// Gets an active dependencies.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal CargoDependency[] GetActiveDependencies()
    {
        return CopyDependencies(activeDependencies);
    }

    /// <summary>
    /// Gets a dependency feature count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetDependencyFeatureCount()
    {
        return dependencyFeatures.Length;
    }

    /// <summary>
    /// Gets a dependency features.
    /// </summary>
    /// <param name="dependencyIndex">The zero-based dependency index.</param>
    /// <returns>The resulting cargo resolved dependency features.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal CargoResolvedDependencyFeatures GetDependencyFeatures(int dependencyIndex)
    {
        if (dependencyIndex < 0 || dependencyIndex >= dependencyFeatures.Length)
        {
            throw new System.ArgumentOutOfRangeException("dependencyIndex");
        }

        return dependencyFeatures[dependencyIndex];
    }

    /// <summary>
    /// Finds the resolved feature set for a dependency package name.
    /// </summary>
    /// <param name="packageName">The package name used by the operation.</param>
    /// <returns>The resulting cargo resolved dependency features.</returns>
    internal CargoResolvedDependencyFeatures? FindDependencyFeatures(string packageName)
    {
        int index = 0;

        while (index < dependencyFeatures.Length)
        {
            if (dependencyFeatures[index].GetPackageName().MatchesText(packageName))
            {
                return dependencyFeatures[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool ContainsText(string[] values, string value)
    {
        int index = 0;

        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string[] CopyStrings(string[] values)
    {
        string[] copy = new string[values.Length];
        int index = 0;

        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new System.ArgumentException("Cargo feature arrays cannot contain null values.");
            }

            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static CargoDependency[] CopyDependencies(CargoDependency[] values)
    {
        CargoDependency[] copy = new CargoDependency[values.Length];
        int index = 0;

        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new System.ArgumentException("Cargo dependency arrays cannot contain null values.");
            }

            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static CargoResolvedDependencyFeatures[] CopyDependencyFeatures(CargoResolvedDependencyFeatures[] values)
    {
        CargoResolvedDependencyFeatures[] copy = new CargoResolvedDependencyFeatures[values.Length];
        int index = 0;

        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new System.ArgumentException("Cargo dependency feature arrays cannot contain null values.");
            }

            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }
}
