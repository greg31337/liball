namespace version;

/// <summary>
/// Represents a rubygems dependency graph edge.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class RubyGemsDependencyGraphEdge
{
    /// <summary>
    /// Stores the <c>parentSpecification</c> value.
    /// </summary>
    public RubyGemsSpecification? parentSpecification;
    /// <summary>
    /// Stores the <c>dependency</c> value.
    /// </summary>
    public RubyGemsDependency dependency;
    /// <summary>
    /// Stores the <c>depth</c> value.
    /// </summary>
    public int depth;

    /// <summary>
    /// Initializes a new RubyGems Dependency Graph Edge instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="parentSpecificationValue">The parent specification value used by the operation.</param>
    /// <param name="dependencyValue">The dependency value used by the operation.</param>
    /// <param name="depthValue">The depth value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public RubyGemsDependencyGraphEdge(
        RubyGemsSpecification? parentSpecificationValue,
        RubyGemsDependency dependencyValue,
        int depthValue)
    {
        System.ArgumentNullException.ThrowIfNull(dependencyValue);

        if (depthValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("depthValue");
        }

        parentSpecification = parentSpecificationValue;
        dependency = dependencyValue;
        depth = depthValue;
    }

    /// <summary>
    /// Creates a root edge.
    /// </summary>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <returns>The resulting rubygems dependency graph edge.</returns>
    internal static RubyGemsDependencyGraphEdge CreateRootEdge(RubyGemsDependency dependency)
    {
        return new RubyGemsDependencyGraphEdge(null, dependency, 0);
    }

    /// <summary>
    /// Creates a specification edge.
    /// </summary>
    /// <param name="parentSpecification">The parent specification used by the operation.</param>
    /// <param name="dependency">The dependency used by the operation.</param>
    /// <param name="depth">The depth used by the operation.</param>
    /// <returns>The resulting rubygems dependency graph edge.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static RubyGemsDependencyGraphEdge CreateSpecificationEdge(
        RubyGemsSpecification parentSpecification,
        RubyGemsDependency dependency,
        int depth)
    {
        System.ArgumentNullException.ThrowIfNull(parentSpecification);

        return new RubyGemsDependencyGraphEdge(parentSpecification, dependency, depth);
    }

    /// <summary>
    /// Reports whether this value has parent.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasParent()
    {
        return parentSpecification != null;
    }

    /// <summary>
    /// Gets a parent specification.
    /// </summary>
    /// <returns>The resulting rubygems specification.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal RubyGemsSpecification GetParentSpecification()
    {
        if (parentSpecification == null)
        {
            throw new System.InvalidOperationException("Root dependency edges do not contain a parent specification.");
        }

        return parentSpecification;
    }

    /// <summary>
    /// Gets a dependency.
    /// </summary>
    /// <returns>The resulting rubygems dependency.</returns>
    internal RubyGemsDependency GetDependency()
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
