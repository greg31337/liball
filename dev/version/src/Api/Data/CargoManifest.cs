namespace version;

/// <summary>
/// Represents a cargo manifest.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class CargoManifest
{
    /// <summary>
    /// Stores the <c>dependencies</c> value.
    /// </summary>
    public CargoDependency[] dependencies;
    /// <summary>
    /// Stores the <c>features</c> value.
    /// </summary>
    public CargoFeature[] features;
    /// <summary>
    /// Stores the <c>registrySources</c> value.
    /// </summary>
    public CargoRegistrySource[] registrySources;
    /// <summary>
    /// Stores the <c>sourceReplacements</c> value.
    /// </summary>
    public CargoSourceReplacement[] sourceReplacements;

    /// <summary>
    /// Initializes a new Cargo Manifest instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="dependencyValues">The dependency values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="featureValues">The feature values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="registrySourceValues">The registry source values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="sourceReplacementValues">The source replacement values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public CargoManifest(
        CargoDependency[] dependencyValues,
        CargoFeature[] featureValues,
        CargoRegistrySource[] registrySourceValues,
        CargoSourceReplacement[] sourceReplacementValues)
    {
        System.ArgumentNullException.ThrowIfNull(dependencyValues);
        System.ArgumentNullException.ThrowIfNull(featureValues);
        System.ArgumentNullException.ThrowIfNull(registrySourceValues);
        System.ArgumentNullException.ThrowIfNull(sourceReplacementValues);

        dependencies = CopyDependencies(dependencyValues);
        features = CopyFeatures(featureValues);
        registrySources = CopyRegistrySources(registrySourceValues);
        sourceReplacements = CopySourceReplacements(sourceReplacementValues);
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
    /// Gets a dependency.
    /// </summary>
    /// <param name="dependencyIndex">The zero-based dependency index.</param>
    /// <returns>The resulting cargo dependency.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal CargoDependency GetDependency(int dependencyIndex)
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
    internal CargoDependency[] GetDependencies()
    {
        return CopyDependencies(dependencies);
    }

    /// <summary>
    /// Gets a feature count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetFeatureCount()
    {
        return features.Length;
    }

    /// <summary>
    /// Gets a feature.
    /// </summary>
    /// <param name="featureIndex">The zero-based feature index.</param>
    /// <returns>The resulting cargo feature.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal CargoFeature GetFeature(int featureIndex)
    {
        if (featureIndex < 0 || featureIndex >= features.Length)
        {
            throw new System.ArgumentOutOfRangeException("featureIndex");
        }

        return features[featureIndex];
    }

    /// <summary>
    /// Gets a features.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal CargoFeature[] GetFeatures()
    {
        return CopyFeatures(features);
    }

    /// <summary>
    /// Gets a registry source count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetRegistrySourceCount()
    {
        return registrySources.Length;
    }

    /// <summary>
    /// Gets a registry source.
    /// </summary>
    /// <param name="registrySourceIndex">The zero-based registry source index.</param>
    /// <returns>The resulting cargo registry source.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal CargoRegistrySource GetRegistrySource(int registrySourceIndex)
    {
        if (registrySourceIndex < 0 || registrySourceIndex >= registrySources.Length)
        {
            throw new System.ArgumentOutOfRangeException("registrySourceIndex");
        }

        return registrySources[registrySourceIndex];
    }

    /// <summary>
    /// Gets a registry sources.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal CargoRegistrySource[] GetRegistrySources()
    {
        return CopyRegistrySources(registrySources);
    }

    /// <summary>
    /// Gets a source replacement count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetSourceReplacementCount()
    {
        return sourceReplacements.Length;
    }

    /// <summary>
    /// Gets a source replacement.
    /// </summary>
    /// <param name="sourceReplacementIndex">The zero-based source replacement index.</param>
    /// <returns>The resulting cargo source replacement.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal CargoSourceReplacement GetSourceReplacement(int sourceReplacementIndex)
    {
        if (sourceReplacementIndex < 0 || sourceReplacementIndex >= sourceReplacements.Length)
        {
            throw new System.ArgumentOutOfRangeException("sourceReplacementIndex");
        }

        return sourceReplacements[sourceReplacementIndex];
    }

    /// <summary>
    /// Gets a source replacements.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal CargoSourceReplacement[] GetSourceReplacements()
    {
        return CopySourceReplacements(sourceReplacements);
    }

    internal static CargoDependency[] CopyDependencies(CargoDependency[] dependencyValues)
    {
        CargoDependency[] copy = new CargoDependency[dependencyValues.Length];
        int index = 0;

        while (index < dependencyValues.Length)
        {
            if (dependencyValues[index] == null)
            {
                throw new System.ArgumentException("Cargo manifest dependency arrays cannot contain null values.");
            }

            copy[index] = dependencyValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static CargoFeature[] CopyFeatures(CargoFeature[] featureValues)
    {
        CargoFeature[] copy = new CargoFeature[featureValues.Length];
        int index = 0;

        while (index < featureValues.Length)
        {
            if (featureValues[index] == null)
            {
                throw new System.ArgumentException("Cargo manifest feature arrays cannot contain null values.");
            }

            copy[index] = featureValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static CargoRegistrySource[] CopyRegistrySources(CargoRegistrySource[] registrySourceValues)
    {
        CargoRegistrySource[] copy = new CargoRegistrySource[registrySourceValues.Length];
        int index = 0;

        while (index < registrySourceValues.Length)
        {
            if (registrySourceValues[index] == null)
            {
                throw new System.ArgumentException("Cargo manifest registry source arrays cannot contain null values.");
            }

            copy[index] = registrySourceValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static CargoSourceReplacement[] CopySourceReplacements(CargoSourceReplacement[] sourceReplacementValues)
    {
        CargoSourceReplacement[] copy = new CargoSourceReplacement[sourceReplacementValues.Length];
        int index = 0;

        while (index < sourceReplacementValues.Length)
        {
            if (sourceReplacementValues[index] == null)
            {
                throw new System.ArgumentException("Cargo manifest source replacement arrays cannot contain null values.");
            }

            copy[index] = sourceReplacementValues[index];
            index = index + 1;
        }

        return copy;
    }
}
