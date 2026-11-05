namespace version;

/// <summary>
/// Represents a cargo dependency graph node.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class CargoDependencyGraphNode
{
    /// <summary>
    /// Stores the <c>packageMetadata</c> value.
    /// </summary>
    public CargoPackageMetadata packageMetadata;
    /// <summary>
    /// Stores the <c>depth</c> value.
    /// </summary>
    public int depth;
    /// <summary>
    /// Stores the <c>direct</c> value.
    /// </summary>
    public bool direct;
    /// <summary>
    /// Stores the <c>selectedFeatures</c> value.
    /// </summary>
    public CargoResolvedDependencyFeatures selectedFeatures;

    /// <summary>
    /// Initializes a new Cargo Dependency Graph Node instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="packageMetadataValue">The package metadata value used by the operation.</param>
    /// <param name="depthValue">The depth value used by the operation.</param>
    /// <param name="directValue">Whether to enable direct value.</param>
    /// <param name="selectedFeaturesValue">The selected features value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public CargoDependencyGraphNode(
        CargoPackageMetadata packageMetadataValue,
        int depthValue,
        bool directValue,
        CargoResolvedDependencyFeatures selectedFeaturesValue)
    {
        System.ArgumentNullException.ThrowIfNull(packageMetadataValue);
        System.ArgumentNullException.ThrowIfNull(selectedFeaturesValue);

        if (depthValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("depthValue");
        }

        packageMetadata = packageMetadataValue;
        depth = depthValue;
        direct = directValue;
        selectedFeatures = selectedFeaturesValue;
    }

    /// <summary>
    /// Gets a package metadata.
    /// </summary>
    /// <returns>The resulting cargo package metadata.</returns>
    internal CargoPackageMetadata GetPackageMetadata()
    {
        return packageMetadata;
    }

    /// <summary>
    /// Gets an identity.
    /// </summary>
    /// <returns>The resulting cargo package version identity.</returns>
    internal CargoPackageVersionIdentity GetIdentity()
    {
        return packageMetadata.GetIdentity();
    }

    /// <summary>
    /// Gets a depth.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetDepth()
    {
        return depth;
    }

    /// <summary>
    /// Reports whether direct.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsDirect()
    {
        return direct;
    }

    /// <summary>
    /// Gets a selected features.
    /// </summary>
    /// <returns>The resulting cargo resolved dependency features.</returns>
    internal CargoResolvedDependencyFeatures GetSelectedFeatures()
    {
        return selectedFeatures;
    }
}
