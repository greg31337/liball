namespace version;

/// <summary>
/// Represents a maven dependency graph edge.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenDependencyGraphEdge
{
    /// <summary>
    /// Stores the <c>parentIdentity</c> value.
    /// </summary>
    public MavenArtifactVersionIdentity? parentIdentity;
    /// <summary>
    /// Stores the <c>dependency</c> value.
    /// </summary>
    public MavenDependency dependency;
    /// <summary>
    /// Stores the <c>depth</c> value.
    /// </summary>
    public int depth;
    /// <summary>
    /// Stores the <c>effectiveScope</c> value.
    /// </summary>
    public MavenDependencyScope effectiveScope;

    /// <summary>
    /// Initializes a new Maven Dependency Graph Edge instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="parentIdentityValue">The parent identity value used by the operation.</param>
    /// <param name="dependencyValue">The dependency value used by the operation.</param>
    /// <param name="depthValue">The depth value used by the operation.</param>
    /// <param name="effectiveScopeValue">The effective scope value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public MavenDependencyGraphEdge(
        MavenArtifactVersionIdentity? parentIdentityValue,
        MavenDependency dependencyValue,
        int depthValue,
        MavenDependencyScope effectiveScopeValue)
    {
        System.ArgumentNullException.ThrowIfNull(dependencyValue);

        if (depthValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("depthValue");
        }

        parentIdentity = parentIdentityValue;
        dependency = dependencyValue;
        depth = depthValue;
        effectiveScope = effectiveScopeValue;
    }

    /// <summary>
    /// Creates a root edge.
    /// </summary>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <returns>The resulting maven dependency graph edge.</returns>
    internal static MavenDependencyGraphEdge CreateRootEdge(MavenDependency dependency)
    {
        return new MavenDependencyGraphEdge(null, dependency, 0, dependency.GetScope());
    }

    /// <summary>
    /// Creates a package edge.
    /// </summary>
    /// <param name="parentIdentity">The parent identity used by the operation.</param>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <param name="depth">The depth used by the operation.</param>
    /// <param name="effectiveScope">The effective scope used by the operation.</param>
    /// <returns>The resulting maven dependency graph edge.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static MavenDependencyGraphEdge CreatePackageEdge(
        MavenArtifactVersionIdentity parentIdentity,
        MavenDependency dependency,
        int depth,
        MavenDependencyScope effectiveScope)
    {
        System.ArgumentNullException.ThrowIfNull(parentIdentity);

        return new MavenDependencyGraphEdge(parentIdentity, dependency, depth, effectiveScope);
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
    /// <returns>The resulting maven artifact version identity.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal MavenArtifactVersionIdentity GetParentIdentity()
    {
        if (parentIdentity == null)
        {
            throw new System.InvalidOperationException("Root Maven dependency edges do not contain a parent identity.");
        }

        return parentIdentity;
    }

    /// <summary>
    /// Gets a dependency.
    /// </summary>
    /// <returns>The resulting maven dependency.</returns>
    internal MavenDependency GetDependency()
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

    /// <summary>
    /// Gets an effective scope.
    /// </summary>
    /// <returns>The resulting maven dependency scope.</returns>
    internal MavenDependencyScope GetEffectiveScope()
    {
        return effectiveScope;
    }
}
