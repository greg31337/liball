namespace version;

/// <summary>
/// Represents a nuget dependency graph node.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetDependencyGraphNode
{
    /// <summary>
    /// Stores the <c>packageMetadata</c> value.
    /// </summary>
    public NuGetSourcePackageMetadata packageMetadata;
    /// <summary>
    /// Stores the <c>depth</c> value.
    /// </summary>
    public int depth;
    /// <summary>
    /// Stores the <c>direct</c> value.
    /// </summary>
    public bool direct;

    /// <summary>
    /// Initializes a new NuGet Dependency Graph Node instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="packageMetadataValue">The package metadata value used by the operation.</param>
    /// <param name="depthValue">The depth value used by the operation.</param>
    /// <param name="directValue">Whether to enable direct value.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public NuGetDependencyGraphNode(NuGetSourcePackageMetadata packageMetadataValue, int depthValue, bool directValue)
    {
        System.ArgumentNullException.ThrowIfNull(packageMetadataValue);

        if (depthValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("depthValue");
        }

        packageMetadata = packageMetadataValue;
        depth = depthValue;
        direct = directValue;
    }

    /// <summary>
    /// Gets a package metadata.
    /// </summary>
    /// <returns>The resulting nuget source package metadata.</returns>
    internal NuGetSourcePackageMetadata GetPackageMetadata()
    {
        return packageMetadata;
    }

    /// <summary>
    /// Gets an identity.
    /// </summary>
    /// <returns>The resulting nuget package version identity.</returns>
    internal NuGetPackageVersionIdentity GetIdentity()
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
}
