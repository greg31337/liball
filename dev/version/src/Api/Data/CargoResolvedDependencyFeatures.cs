namespace version;

/// <summary>
/// Represents a cargo resolved dependency features.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class CargoResolvedDependencyFeatures
{
    /// <summary>
    /// Stores the <c>packageName</c> value.
    /// </summary>
    public CargoPackageName packageName;
    /// <summary>
    /// Stores the <c>dependencyKind</c> value.
    /// </summary>
    public CargoDependencyKind dependencyKind;
    /// <summary>
    /// Stores the <c>defaultFeatures</c> value.
    /// </summary>
    public bool defaultFeatures;
    /// <summary>
    /// Stores the <c>features</c> value.
    /// </summary>
    public string[] features;

    /// <summary>
    /// Initializes a new Cargo Resolved Dependency Features instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="packageNameValue">The package name value used by the operation.</param>
    /// <param name="dependencyKindValue">The dependency kind value used by the operation.</param>
    /// <param name="defaultFeaturesValue">Whether to enable default features value.</param>
    /// <param name="featureValues">The feature values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public CargoResolvedDependencyFeatures(
        CargoPackageName packageNameValue,
        CargoDependencyKind dependencyKindValue,
        bool defaultFeaturesValue,
        string[] featureValues)
    {
        System.ArgumentNullException.ThrowIfNull(packageNameValue);
        System.ArgumentNullException.ThrowIfNull(featureValues);
        ValidateDependencyKind(dependencyKindValue);

        packageName = packageNameValue;
        dependencyKind = dependencyKindValue;
        defaultFeatures = defaultFeaturesValue;
        features = CopyFeatures(featureValues);
    }

    /// <summary>
    /// Gets a package name.
    /// </summary>
    /// <returns>The resulting cargo package name.</returns>
    internal CargoPackageName GetPackageName()
    {
        return packageName;
    }

    /// <summary>
    /// Gets a dependency kind.
    /// </summary>
    /// <returns>The resulting cargo dependency kind.</returns>
    internal CargoDependencyKind GetDependencyKind()
    {
        return dependencyKind;
    }

    /// <summary>
    /// Reports whether default features.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool UsesDefaultFeatures()
    {
        return defaultFeatures;
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
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal string GetFeature(int featureIndex)
    {
        if (featureIndex < 0 || featureIndex >= features.Length)
        {
            throw new System.ArgumentOutOfRangeException("featureIndex");
        }

        return features[featureIndex];
    }

    /// <summary>
    /// Reports whether this value has feature.
    /// </summary>
    /// <param name="featureName">The feature name used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool HasFeature(string featureName)
    {
        System.ArgumentNullException.ThrowIfNull(featureName);

        int index = 0;

        while (index < features.Length)
        {
            if (features[index] == featureName)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Gets a features.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal string[] GetFeatures()
    {
        return CopyFeatures(features);
    }

    internal static void ValidateDependencyKind(CargoDependencyKind dependencyKindValue)
    {
        if (dependencyKindValue == CargoDependencyKind.Normal)
        {
            return;
        }

        if (dependencyKindValue == CargoDependencyKind.Development)
        {
            return;
        }

        if (dependencyKindValue == CargoDependencyKind.Build)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("dependencyKindValue");
    }

    internal static string[] CopyFeatures(string[] featureValues)
    {
        string[] copy = new string[featureValues.Length];
        int index = 0;

        while (index < featureValues.Length)
        {
            CargoFeature.ValidateFeatureToken(featureValues[index], "Cargo resolved dependency feature arrays cannot contain empty or invalid values.");
            copy[index] = featureValues[index];
            index = index + 1;
        }

        return copy;
    }
}
