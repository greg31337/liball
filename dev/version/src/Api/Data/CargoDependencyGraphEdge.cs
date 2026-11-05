namespace version;

/// <summary>
/// Represents a cargo dependency graph edge.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class CargoDependencyGraphEdge
{
    /// <summary>
    /// Stores the <c>parentIdentity</c> value.
    /// </summary>
    public CargoPackageVersionIdentity? parentIdentity;
    /// <summary>
    /// Stores the <c>dependency</c> value.
    /// </summary>
    public CargoDependency dependency;
    /// <summary>
    /// Stores the <c>depth</c> value.
    /// </summary>
    public int depth;

    /// <summary>
    /// Initializes a new Cargo Dependency Graph Edge instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="parentIdentityValue">The parent identity value used by the operation.</param>
    /// <param name="dependencyValue">The dependency value used by the operation.</param>
    /// <param name="depthValue">The depth value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public CargoDependencyGraphEdge(CargoPackageVersionIdentity? parentIdentityValue, CargoDependency dependencyValue, int depthValue)
    {
        System.ArgumentNullException.ThrowIfNull(dependencyValue);

        if (depthValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("depthValue");
        }

        parentIdentity = parentIdentityValue;
        dependency = dependencyValue;
        depth = depthValue;
    }

    /// <summary>
    /// Creates a root edge.
    /// </summary>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <returns>The resulting cargo dependency graph edge.</returns>
    internal static CargoDependencyGraphEdge CreateRootEdge(CargoDependency dependency)
    {
        return new CargoDependencyGraphEdge(null, dependency, 0);
    }

    /// <summary>
    /// Creates a package edge.
    /// </summary>
    /// <param name="parentIdentity">The parent identity used by the operation.</param>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <param name="depth">The depth used by the operation.</param>
    /// <returns>The resulting cargo dependency graph edge.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static CargoDependencyGraphEdge CreatePackageEdge(
        CargoPackageVersionIdentity parentIdentity,
        CargoDependency dependency,
        int depth)
    {
        System.ArgumentNullException.ThrowIfNull(parentIdentity);

        return new CargoDependencyGraphEdge(parentIdentity, dependency, depth);
    }

    /// <summary>
    /// Reports whether this value has parent.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasParent()
    {
        return parentIdentity != null;
    }

    /// <summary>
    /// Gets a parent identity.
    /// </summary>
    /// <returns>The resulting cargo package version identity.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal CargoPackageVersionIdentity GetParentIdentity()
    {
        if (parentIdentity == null)
        {
            throw new System.InvalidOperationException("Root dependency edges do not contain a parent identity.");
        }

        return parentIdentity;
    }

    /// <summary>
    /// Gets a dependency.
    /// </summary>
    /// <returns>The resulting cargo dependency.</returns>
    internal CargoDependency GetDependency()
    {
        return dependency;
    }

    /// <summary>
    /// Gets a depth.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetDepth()
    {
        return depth;
    }
}
