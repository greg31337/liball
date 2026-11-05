namespace version;

/// <summary>
/// Represents a cargo package metadata.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class CargoPackageMetadata
{
    /// <summary>
    /// Stores the <c>identity</c> value.
    /// </summary>
    public CargoPackageVersionIdentity identity;
    /// <summary>
    /// Stores the <c>dependencies</c> value.
    /// </summary>
    public CargoDependency[] dependencies;
    /// <summary>
    /// Stores the <c>features</c> value.
    /// </summary>
    public CargoFeature[] features;
    /// <summary>
    /// Stores the <c>registrySource</c> value.
    /// </summary>
    public CargoRegistrySource registrySource;
    /// <summary>
    /// Stores the <c>yanked</c> value.
    /// </summary>
    public bool yanked;

    /// <summary>
    /// Initializes a new Cargo Package Metadata instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="identityValue">The identity value used by the operation.</param>
    /// <param name="dependencyValues">The dependency values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="featureValues">The feature values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="registrySourceValue">The registry source value used by the operation.</param>
    /// <param name="yankedValue">Whether to enable yanked value.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public CargoPackageMetadata(
        CargoPackageVersionIdentity identityValue,
        CargoDependency[] dependencyValues,
        CargoFeature[] featureValues,
        CargoRegistrySource registrySourceValue,
        bool yankedValue)
    {
        System.ArgumentNullException.ThrowIfNull(identityValue);
        System.ArgumentNullException.ThrowIfNull(dependencyValues);
        System.ArgumentNullException.ThrowIfNull(featureValues);
        System.ArgumentNullException.ThrowIfNull(registrySourceValue);

        identity = identityValue;
        dependencies = CopyDependencies(dependencyValues);
        features = CopyFeatures(featureValues);
        registrySource = registrySourceValue;
        yanked = yankedValue;
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="identity">The identity used by the operation.</param>
    /// <param name="dependencies">The dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="features">The features to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="registrySource">The registry source used by the operation.</param>
    /// <returns>The resulting cargo package metadata.</returns>
    internal static CargoPackageMetadata Create(
        CargoPackageVersionIdentity identity,
        CargoDependency[] dependencies,
        CargoFeature[] features,
        CargoRegistrySource registrySource)
    {
        return new CargoPackageMetadata(identity, dependencies, features, registrySource, false);
    }

    /// <summary>
    /// Gets an identity.
    /// </summary>
    /// <returns>The resulting cargo package version identity.</returns>
    internal CargoPackageVersionIdentity GetIdentity()
    {
        return identity;
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
    /// Gets a registry source.
    /// </summary>
    /// <returns>The resulting cargo registry source.</returns>
    internal CargoRegistrySource GetRegistrySource()
    {
        return registrySource;
    }

    /// <summary>
    /// Reports whether yanked.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsYanked()
    {
        return yanked;
    }

    internal static CargoDependency[] CopyDependencies(CargoDependency[] dependencyValues)
    {
        CargoDependency[] copy = new CargoDependency[dependencyValues.Length];
        int index = 0;

        while (index < dependencyValues.Length)
        {
            if (dependencyValues[index] == null)
            {
                throw new System.ArgumentException("Cargo dependency arrays cannot contain null values.");
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
                throw new System.ArgumentException("Cargo feature arrays cannot contain null values.");
            }

            copy[index] = featureValues[index];
            index = index + 1;
        }

        return copy;
    }
}
