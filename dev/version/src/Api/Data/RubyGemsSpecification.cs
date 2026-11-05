namespace version;

/// <summary>
/// Represents a rubygems specification.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class RubyGemsSpecification
{
    /// <summary>
    /// Stores the <c>name</c> value.
    /// </summary>
    public string name;
    /// <summary>
    /// Stores the <c>version</c> value.
    /// </summary>
    public UniversalVersion version;
    /// <summary>
    /// Stores the <c>platform</c> value.
    /// </summary>
    public RubyGemsPlatform platform;
    /// <summary>
    /// Stores the <c>dependencies</c> value.
    /// </summary>
    public RubyGemsDependency[] dependencies;
    /// <summary>
    /// Stores the <c>source</c> value.
    /// </summary>
    public RubyGemsSource source;

    /// <summary>
    /// Initializes a new RubyGems Specification instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="nameValue">The name value used by the operation.</param>
    /// <param name="versionValue">The version value used by the operation.</param>
    /// <param name="platformValue">The platform value used by the operation.</param>
    /// <param name="dependencyValues">The dependency values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="sourceValue">The source value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public RubyGemsSpecification(
        string nameValue,
        UniversalVersion versionValue,
        RubyGemsPlatform platformValue,
        RubyGemsDependency[] dependencyValues,
        RubyGemsSource sourceValue)
    {
        RubyGemsDependency.ValidateName(nameValue, "RubyGems specification name cannot be empty or contain invalid characters.");
        System.ArgumentNullException.ThrowIfNull(versionValue);
        System.ArgumentNullException.ThrowIfNull(platformValue);
        System.ArgumentNullException.ThrowIfNull(dependencyValues);
        System.ArgumentNullException.ThrowIfNull(sourceValue);

        if (versionValue.GetDialect() != VersionDialect.RubyGems)
        {
            throw new System.ArgumentException("RubyGems specification version must use the RubyGems dialect.");
        }

        name = nameValue;
        version = versionValue;
        platform = platformValue;
        dependencies = CopyDependencies(dependencyValues);
        source = sourceValue;
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="name">The name used by the operation.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="platform">The platform used by the operation.</param>
    /// <param name="dependencies">The dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="source">The source used by the operation.</param>
    /// <returns>The resulting rubygems specification.</returns>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static RubyGemsSpecification Create(
        string name,
        string versionText,
        RubyGemsPlatform platform,
        RubyGemsDependency[] dependencies,
        RubyGemsSource source)
    {
        VersionParseResult result = RubyGemsVersions.Parse(versionText);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid RubyGems specification version.");
        }

        return new RubyGemsSpecification(name, result.GetVersion(), platform, dependencies, source);
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
    /// Gets a version.
    /// </summary>
    /// <returns>The resulting universal version.</returns>
    internal UniversalVersion GetVersion()
    {
        return version;
    }

    /// <summary>
    /// Gets a version text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetVersionText()
    {
        return version.GetOriginalText();
    }

    /// <summary>
    /// Gets a platform.
    /// </summary>
    /// <returns>The resulting rubygems platform.</returns>
    internal RubyGemsPlatform GetPlatform()
    {
        return platform;
    }

    /// <summary>
    /// Gets a source.
    /// </summary>
    /// <returns>The resulting rubygems source.</returns>
    internal RubyGemsSource GetSource()
    {
        return source;
    }

    /// <summary>
    /// Gets a dependency count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetDependencyCount()
    {
        return dependencies.Length;
    }

    /// <summary>
    /// Gets a dependency.
    /// </summary>
    /// <param name="dependencyIndex">The zero-based dependency index.</param>
    /// <returns>The resulting rubygems dependency.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal RubyGemsDependency GetDependency(int dependencyIndex)
    {
        if (dependencyIndex < 0 || dependencyIndex >= dependencies.Length)
        {
            throw new System.ArgumentOutOfRangeException("dependencyIndex");
        }

        return dependencies[dependencyIndex];
    }

    /// <summary>
    /// Gets a dependencies.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal RubyGemsDependency[] GetDependencies()
    {
        return CopyDependencies(dependencies);
    }

    /// <summary>
    /// Gets a full name.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetFullName()
    {
        if (platform.IsRuby())
        {
            return name + "-" + version.GetOriginalText();
        }

        return name + "-" + version.GetOriginalText() + "-" + platform.GetValue();
    }

    /// <summary>
    /// Reports whether the version is a prerelease.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsPrerelease()
    {
        return version.IsPrerelease();
    }

    /// <summary>
    /// Reports whether this value matches the supplied normalized name.
    /// </summary>
    /// <param name="nameText">The name text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool MatchesName(string nameText)
    {
        System.ArgumentNullException.ThrowIfNull(nameText);

        return name == nameText;
    }

    /// <summary>
    /// Reports whether this value has same identity.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool HasSameIdentity(RubyGemsSpecification other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return name == other.GetName()
            && RubyGemsVersions.Compare(version.GetOriginalText(), other.GetVersionText()) == 0
            && platform.MatchesText(other.GetPlatform().GetValue());
    }

    internal static RubyGemsDependency[] CopyDependencies(RubyGemsDependency[] dependencyValues)
    {
        RubyGemsDependency[] copy = new RubyGemsDependency[dependencyValues.Length];
        int index = 0;

        while (index < dependencyValues.Length)
        {
            if (dependencyValues[index] == null)
            {
                throw new System.ArgumentException("RubyGems dependency arrays cannot contain null values.");
            }

            copy[index] = dependencyValues[index];
            index = index + 1;
        }

        return copy;
    }
}
