namespace version;

/// <summary>
/// Represents a nuget dependency graph edge.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetDependencyGraphEdge
{
    /// <summary>
    /// Stores the <c>parentIdentity</c> value.
    /// </summary>
    public NuGetPackageVersionIdentity? parentIdentity;
    /// <summary>
    /// Stores the <c>constraint</c> value.
    /// </summary>
    public NuGetDependencyRangeConstraint constraint;

    /// <summary>
    /// Initializes a new NuGet Dependency Graph Edge instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="parentIdentityValue">The parent identity value used by the operation.</param>
    /// <param name="constraintValue">The constraint value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public NuGetDependencyGraphEdge(NuGetPackageVersionIdentity? parentIdentityValue, NuGetDependencyRangeConstraint constraintValue)
    {
        System.ArgumentNullException.ThrowIfNull(constraintValue);

        parentIdentity = parentIdentityValue;
        constraint = constraintValue;
    }

    /// <summary>
    /// Creates a root edge.
    /// </summary>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <returns>The resulting nuget dependency graph edge.</returns>
    internal static NuGetDependencyGraphEdge CreateRootEdge(NuGetDependency dependency)
    {
        return new NuGetDependencyGraphEdge(null, NuGetDependencyRangeConstraint.Create(dependency, 0, true));
    }

    /// <summary>
    /// Creates a package edge.
    /// </summary>
    /// <param name="parentIdentity">The parent identity used by the operation.</param>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <param name="depth">The depth used by the operation.</param>
    /// <returns>The resulting nuget dependency graph edge.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static NuGetDependencyGraphEdge CreatePackageEdge(NuGetPackageVersionIdentity parentIdentity, NuGetDependency dependency, int depth)
    {
        System.ArgumentNullException.ThrowIfNull(parentIdentity);

        return new NuGetDependencyGraphEdge(parentIdentity, NuGetDependencyRangeConstraint.Create(dependency, depth, false));
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
    /// <returns>The resulting nuget package version identity.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal NuGetPackageVersionIdentity GetParentIdentity()
    {
        if (parentIdentity == null)
        {
            throw new System.InvalidOperationException("Root dependency edges do not contain a parent identity.");
        }

        return parentIdentity;
    }

    /// <summary>
    /// Gets a constraint.
    /// </summary>
    /// <returns>The resulting nuget dependency range constraint.</returns>
    internal NuGetDependencyRangeConstraint GetConstraint()
    {
        return constraint;
    }
}
