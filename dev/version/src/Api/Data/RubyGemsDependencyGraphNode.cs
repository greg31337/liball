namespace version;

/// <summary>
/// Represents a rubygems dependency graph node.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class RubyGemsDependencyGraphNode
{
    /// <summary>
    /// Stores the <c>specification</c> value.
    /// </summary>
    public RubyGemsSpecification specification;
    /// <summary>
    /// Stores the <c>depth</c> value.
    /// </summary>
    public int depth;
    /// <summary>
    /// Stores the <c>direct</c> value.
    /// </summary>
    public bool direct;

    /// <summary>
    /// Initializes a new RubyGems Dependency Graph Node instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="specificationValue">The specification value used by the operation.</param>
    /// <param name="depthValue">The depth value used by the operation.</param>
    /// <param name="directValue">Whether to enable direct value.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    public RubyGemsDependencyGraphNode(RubyGemsSpecification specificationValue, int depthValue, bool directValue)
    {
        System.ArgumentNullException.ThrowIfNull(specificationValue);

        if (depthValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("depthValue");
        }

        specification = specificationValue;
        depth = depthValue;
        direct = directValue;
    }

    /// <summary>
    /// Gets a specification.
    /// </summary>
    /// <returns>The resulting rubygems specification.</returns>
    internal RubyGemsSpecification GetSpecification()
    {
        return specification;
    }

    /// <summary>
    /// Gets a name.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetName()
    {
        return specification.GetName();
    }

    /// <summary>
    /// Gets a version text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetVersionText()
    {
        return specification.GetVersionText();
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
