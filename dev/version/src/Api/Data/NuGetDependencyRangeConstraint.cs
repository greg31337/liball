namespace version;

/// <summary>
/// Represents a nuget dependency range constraint.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetDependencyRangeConstraint
{
    /// <summary>
    /// Stores the <c>packageId</c> value.
    /// </summary>
    public NuGetPackageId packageId;
    /// <summary>
    /// Stores the <c>versionRange</c> value.
    /// </summary>
    public VersionRange versionRange;
    /// <summary>
    /// Stores the <c>versionRangeText</c> value.
    /// </summary>
    public string versionRangeText;
    /// <summary>
    /// Stores the <c>depth</c> value.
    /// </summary>
    public int depth;
    /// <summary>
    /// Stores the <c>direct</c> value.
    /// </summary>
    public bool direct;

    /// <summary>
    /// Initializes a new NuGet Dependency Range Constraint instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="packageIdValue">The package id value used by the operation.</param>
    /// <param name="versionRangeValue">The version range value used by the operation.</param>
    /// <param name="versionRangeTextValue">The version range text value to process.</param>
    /// <param name="depthValue">The depth value used by the operation.</param>
    /// <param name="directValue">Whether to enable direct value.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public NuGetDependencyRangeConstraint(NuGetPackageId packageIdValue, VersionRange versionRangeValue, string versionRangeTextValue, int depthValue, bool directValue)
    {
        System.ArgumentNullException.ThrowIfNull(packageIdValue);
        System.ArgumentNullException.ThrowIfNull(versionRangeValue);
        System.ArgumentNullException.ThrowIfNull(versionRangeTextValue);

        if (depthValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("depthValue");
        }

        packageId = packageIdValue;
        versionRange = versionRangeValue;
        versionRangeText = versionRangeTextValue;
        depth = depthValue;
        direct = directValue;
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <param name="depth">The depth used by the operation.</param>
    /// <param name="direct">Whether to enable direct.</param>
    /// <returns>The resulting nuget dependency range constraint.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static NuGetDependencyRangeConstraint Create(NuGetDependency dependency, int depth, bool direct)
    {
        System.ArgumentNullException.ThrowIfNull(dependency);

        return new NuGetDependencyRangeConstraint(
            dependency.GetPackageId(),
            dependency.GetVersionRange(),
            dependency.GetVersionRangeText(),
            depth,
            direct);
    }

    /// <summary>
    /// Gets a package id.
    /// </summary>
    /// <returns>The resulting nuget package id.</returns>
    internal NuGetPackageId GetPackageId()
    {
        return packageId;
    }

    /// <summary>
    /// Gets a version range.
    /// </summary>
    /// <returns>The resulting version range.</returns>
    internal VersionRange GetVersionRange()
    {
        return versionRange;
    }

    /// <summary>
    /// Gets a version range text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetVersionRangeText()
    {
        return versionRangeText;
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
    /// Reports whether .
    /// </summary>
    /// <param name="identity">The identity used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool Allows(NuGetPackageVersionIdentity identity)
    {
        System.ArgumentNullException.ThrowIfNull(identity);

        if (!packageId.EqualsId(identity.GetPackageId()))
        {
            return false;
        }

        return versionRange.ContainsVersionWithPrerelease(identity.GetVersion(), true);
    }
}
