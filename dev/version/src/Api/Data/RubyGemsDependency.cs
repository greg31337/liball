namespace version;

/// <summary>
/// Represents a rubygems dependency.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class RubyGemsDependency
{
    /// <summary>
    /// Stores the <c>name</c> value.
    /// </summary>
    public string name;
    /// <summary>
    /// Stores the <c>requirements</c> value.
    /// </summary>
    public RubyGemsRequirementList requirements;
    /// <summary>
    /// Stores the <c>dependencyType</c> value.
    /// </summary>
    public RubyGemsDependencyType dependencyType;

    /// <summary>
    /// Initializes a new RubyGems Dependency instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="nameValue">The name value used by the operation.</param>
    /// <param name="requirementValues">The requirement values used by the operation.</param>
    /// <param name="dependencyTypeValue">The dependency type value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public RubyGemsDependency(string nameValue, RubyGemsRequirementList requirementValues, RubyGemsDependencyType dependencyTypeValue)
    {
        ValidateName(nameValue, "RubyGems dependency name cannot be empty or contain invalid characters.");
        System.ArgumentNullException.ThrowIfNull(requirementValues);
        ValidateDependencyType(dependencyTypeValue);

        name = nameValue;
        requirements = requirementValues;
        dependencyType = dependencyTypeValue;
    }

    /// <summary>
    /// Creates a runtime.
    /// </summary>
    /// <param name="name">The name used by the operation.</param>
    /// <param name="requirementText">The requirement text to process.</param>
    /// <returns>The resulting rubygems dependency.</returns>
    internal static RubyGemsDependency CreateRuntime(string name, string requirementText)
    {
        return new RubyGemsDependency(name, RubyGemsRequirementList.Create(requirementText), RubyGemsDependencyType.Runtime);
    }

    /// <summary>
    /// Creates a development.
    /// </summary>
    /// <param name="name">The name used by the operation.</param>
    /// <param name="requirementText">The requirement text to process.</param>
    /// <returns>The resulting rubygems dependency.</returns>
    internal static RubyGemsDependency CreateDevelopment(string name, string requirementText)
    {
        return new RubyGemsDependency(name, RubyGemsRequirementList.Create(requirementText), RubyGemsDependencyType.Development);
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
    /// Gets a requirements.
    /// </summary>
    /// <returns>The resulting rubygems requirement list.</returns>
    internal RubyGemsRequirementList GetRequirements()
    {
        return requirements;
    }

    /// <summary>
    /// Gets a dependency type.
    /// </summary>
    /// <returns>The resulting rubygems dependency type.</returns>
    internal RubyGemsDependencyType GetDependencyType()
    {
        return dependencyType;
    }

    /// <summary>
    /// Reports whether runtime.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsRuntime()
    {
        return dependencyType == RubyGemsDependencyType.Runtime;
    }

    /// <summary>
    /// Reports whether development.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsDevelopment()
    {
        return dependencyType == RubyGemsDependencyType.Development;
    }

    /// <summary>
    /// Reports whether version text.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool AllowsVersionText(string versionText)
    {
        return requirements.AllowsVersionText(versionText);
    }

    /// <summary>
    /// Reports whether specification.
    /// </summary>
    /// <param name="specification">The specification used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool AllowsSpecification(RubyGemsSpecification specification)
    {
        System.ArgumentNullException.ThrowIfNull(specification);

        return specification.MatchesName(name) && requirements.AllowsVersion(specification.GetVersion());
    }

    /// <summary>
    /// Validates a name.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="message">The message used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static void ValidateName(string text, string message)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0 || !VersionChar.IsAlphaNumeric(text[0]))
        {
            throw new System.ArgumentException(message);
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!VersionChar.IsAlphaNumeric(text[index])
                && text[index] != '-'
                && text[index] != '_'
                && text[index] != '.')
            {
                throw new System.ArgumentException(message);
            }

            index = index + 1;
        }
    }

    internal static void ValidateDependencyType(RubyGemsDependencyType dependencyTypeValue)
    {
        if (dependencyTypeValue == RubyGemsDependencyType.Runtime)
        {
            return;
        }

        if (dependencyTypeValue == RubyGemsDependencyType.Development)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("dependencyTypeValue");
    }
}
