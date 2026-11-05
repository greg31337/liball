namespace version;

/// <summary>
/// Represents a rubygems dependency exclusion.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class RubyGemsDependencyExclusion
{
    /// <summary>
    /// Stores the <c>name</c> value.
    /// </summary>
    public string name;
    /// <summary>
    /// Stores the <c>requirements</c> value.
    /// </summary>
    public RubyGemsRequirementList? requirements;

    /// <summary>
    /// Initializes a new RubyGems Dependency Exclusion instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="nameValue">The name value used by the operation.</param>
    /// <param name="requirementValues">The requirement values used by the operation, or null to exclude every version.</param>
    public RubyGemsDependencyExclusion(string nameValue, RubyGemsRequirementList? requirementValues)
    {
        RubyGemsDependency.ValidateName(nameValue, "RubyGems dependency exclusion name cannot be empty or contain invalid characters.");

        name = nameValue;
        requirements = requirementValues;
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="name">The name used by the operation.</param>
    /// <returns>The resulting rubygems dependency exclusion.</returns>
    internal static RubyGemsDependencyExclusion Create(string name)
    {
        return new RubyGemsDependencyExclusion(name, null);
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="name">The name used by the operation.</param>
    /// <param name="requirementText">The requirement text to process.</param>
    /// <returns>The resulting rubygems dependency exclusion.</returns>
    internal static RubyGemsDependencyExclusion Create(string name, string requirementText)
    {
        return new RubyGemsDependencyExclusion(name, RubyGemsRequirementList.Create(requirementText));
    }

    /// <summary>
    /// Gets a name.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetName()
    {
        return name;
    }

    /// <summary>
    /// Reports whether this value has requirements.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasRequirements()
    {
        return requirements != null;
    }

    /// <summary>
    /// Gets a requirements.
    /// </summary>
    /// <returns>The resulting rubygems requirement list.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal RubyGemsRequirementList GetRequirements()
    {
        if (requirements == null)
        {
            throw new System.InvalidOperationException("RubyGems dependency exclusion does not contain requirements.");
        }

        return requirements;
    }

    /// <summary>
    /// Reports whether this exclusion removes the supplied gem specification.
    /// </summary>
    /// <param name="specification">The specification used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool ExcludesSpecification(RubyGemsSpecification specification)
    {
        System.ArgumentNullException.ThrowIfNull(specification);

        if (!specification.MatchesName(name))
        {
            return false;
        }

        if (requirements == null)
        {
            return true;
        }

        return requirements.AllowsVersion(specification.GetVersion());
    }
}
