namespace version;

/// <summary>
/// Represents a maven dependency graph node.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenDependencyGraphNode
{
    /// <summary>
    /// Stores the <c>model</c> value.
    /// </summary>
    public MavenDependencyModel model;
    /// <summary>
    /// Stores the <c>depth</c> value.
    /// </summary>
    public int depth;
    /// <summary>
    /// Stores the <c>direct</c> value.
    /// </summary>
    public bool direct;
    /// <summary>
    /// Stores the <c>effectiveScope</c> value.
    /// </summary>
    public MavenDependencyScope effectiveScope;

    /// <summary>
    /// Initializes a new Maven Dependency Graph Node instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="modelValue">The model value used by the operation.</param>
    /// <param name="depthValue">The depth value used by the operation.</param>
    /// <param name="directValue">Whether to enable direct value.</param>
    /// <param name="effectiveScopeValue">The effective scope value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public MavenDependencyGraphNode(
        MavenDependencyModel modelValue,
        int depthValue,
        bool directValue,
        MavenDependencyScope effectiveScopeValue)
    {
        System.ArgumentNullException.ThrowIfNull(modelValue);

        if (depthValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("depthValue");
        }

        model = modelValue;
        depth = depthValue;
        direct = directValue;
        effectiveScope = effectiveScopeValue;
    }

    /// <summary>
    /// Gets a model.
    /// </summary>
    /// <returns>The resulting maven dependency model.</returns>
    internal MavenDependencyModel GetModel()
    {
        return model;
    }

    /// <summary>
    /// Gets an identity.
    /// </summary>
    /// <returns>The resulting maven artifact version identity.</returns>
    internal MavenArtifactVersionIdentity GetIdentity()
    {
        return model.GetIdentity();
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
    /// Gets an effective scope.
    /// </summary>
    /// <returns>The resulting maven dependency scope.</returns>
    internal MavenDependencyScope GetEffectiveScope()
    {
        return effectiveScope;
    }
}
