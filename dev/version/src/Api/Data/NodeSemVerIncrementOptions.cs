namespace version;

/// <summary>
/// Configures node semver increment behavior.
/// </summary>
/// <remarks>
/// Configure behavior through the public fields before passing the value to an operation.
/// </remarks>
public class NodeSemVerIncrementOptions
{
    /// <summary>
    /// Stores the <c>releaseType</c> value.
    /// </summary>
    public NodeSemVerIncrementReleaseType releaseType;
    /// <summary>
    /// Stores the <c>identifier</c> value.
    /// </summary>
    public string identifier;
    /// <summary>
    /// Stores the <c>identifierBase</c> value.
    /// </summary>
    public int identifierBase;
    /// <summary>
    /// Stores the <c>identifierBaseEnabled</c> value.
    /// </summary>
    public bool identifierBaseEnabled;
    /// <summary>
    /// Stores the <c>loose</c> value.
    /// </summary>
    public bool loose;
    /// <summary>
    /// Stores the <c>includePrerelease</c> value.
    /// </summary>
    public bool includePrerelease;

    /// <summary>
    /// Initializes a new Node SemVer Increment Options instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="releaseTypeValue">The release type value used by the operation.</param>
    public NodeSemVerIncrementOptions(NodeSemVerIncrementReleaseType releaseTypeValue)
    {
        ValidateReleaseType(releaseTypeValue);

        releaseType = releaseTypeValue;
        identifier = "";
        identifierBase = 0;
        identifierBaseEnabled = true;
        loose = false;
        includePrerelease = false;
    }

    /// <summary>
    /// Creates options for a patch increment using the default settings.
    /// </summary>
    /// <returns>The resulting node semver increment options.</returns>
    internal static NodeSemVerIncrementOptions CreateDefault()
    {
        return new NodeSemVerIncrementOptions(NodeSemVerIncrementReleaseType.Patch);
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="releaseType">The release type used by the operation.</param>
    /// <returns>The resulting node semver increment options.</returns>
    internal static NodeSemVerIncrementOptions Create(NodeSemVerIncrementReleaseType releaseType)
    {
        return new NodeSemVerIncrementOptions(releaseType);
    }

    /// <summary>
    /// Creates a with identifier.
    /// </summary>
    /// <param name="releaseType">The release type used by the operation.</param>
    /// <param name="identifier">The identifier used by the operation.</param>
    /// <returns>The resulting node semver increment options.</returns>
    internal static NodeSemVerIncrementOptions CreateWithIdentifier(NodeSemVerIncrementReleaseType releaseType, string identifier)
    {
        NodeSemVerIncrementOptions options = new NodeSemVerIncrementOptions(releaseType);
        options.SetIdentifier(identifier);
        return options;
    }

    /// <summary>
    /// Creates a with identifier base.
    /// </summary>
    /// <param name="releaseType">The release type used by the operation.</param>
    /// <param name="identifier">The identifier used by the operation.</param>
    /// <param name="identifierBase">The identifier base used by the operation.</param>
    /// <returns>The resulting node semver increment options.</returns>
    internal static NodeSemVerIncrementOptions CreateWithIdentifierBase(NodeSemVerIncrementReleaseType releaseType, string identifier, int identifierBase)
    {
        NodeSemVerIncrementOptions options = CreateWithIdentifier(releaseType, identifier);
        options.SetIdentifierBase(identifierBase);
        return options;
    }

    /// <summary>
    /// Creates a loose.
    /// </summary>
    /// <param name="releaseType">The release type used by the operation.</param>
    /// <returns>The resulting node semver increment options.</returns>
    internal static NodeSemVerIncrementOptions CreateLoose(NodeSemVerIncrementReleaseType releaseType)
    {
        NodeSemVerIncrementOptions options = new NodeSemVerIncrementOptions(releaseType);
        options.SetLoose(true);
        return options;
    }

    /// <summary>
    /// Creates a with prerelease inclusion.
    /// </summary>
    /// <param name="releaseType">The release type used by the operation.</param>
    /// <param name="includePrerelease">Whether to enable include prerelease.</param>
    /// <returns>The resulting node semver increment options.</returns>
    internal static NodeSemVerIncrementOptions CreateWithPrereleaseInclusion(NodeSemVerIncrementReleaseType releaseType, bool includePrerelease)
    {
        NodeSemVerIncrementOptions options = new NodeSemVerIncrementOptions(releaseType);
        options.SetIncludePrerelease(includePrerelease);
        return options;
    }

    /// <summary>
    /// Gets a release type.
    /// </summary>
    /// <returns>The resulting node semver increment release type.</returns>
    internal NodeSemVerIncrementReleaseType GetReleaseType()
    {
        return releaseType;
    }

    /// <summary>
    /// Updates the release type setting.
    /// </summary>
    /// <param name="releaseTypeValue">The release type value used by the operation.</param>
    internal void SetReleaseType(NodeSemVerIncrementReleaseType releaseTypeValue)
    {
        ValidateReleaseType(releaseTypeValue);
        releaseType = releaseTypeValue;
    }

    /// <summary>
    /// Reports whether this value has identifier.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasIdentifier()
    {
        return identifier.Length > 0;
    }

    /// <summary>
    /// Gets an identifier.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetIdentifier()
    {
        return identifier;
    }

    /// <summary>
    /// Updates the identifier setting.
    /// </summary>
    /// <param name="identifierValue">The identifier value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal void SetIdentifier(string identifierValue)
    {
        System.ArgumentNullException.ThrowIfNull(identifierValue);
        ValidateIdentifier(identifierValue);
        identifier = identifierValue;
    }

    /// <summary>
    /// Clears the prerelease identifier used by subsequent increment operations.
    /// </summary>
    internal void ClearIdentifier()
    {
        identifier = "";
    }

    /// <summary>
    /// Reports whether identifier base enabled.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsIdentifierBaseEnabled()
    {
        return identifierBaseEnabled;
    }

    /// <summary>
    /// Gets an identifier base.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetIdentifierBase()
    {
        return identifierBase;
    }

    /// <summary>
    /// Updates the identifier base setting.
    /// </summary>
    /// <param name="identifierBaseValue">The identifier base value used by the operation.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal void SetIdentifierBase(int identifierBaseValue)
    {
        if (identifierBaseValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("identifierBaseValue");
        }

        identifierBase = identifierBaseValue;
        identifierBaseEnabled = true;
    }

    /// <summary>
    /// Disables automatic numeric suffixes for prerelease identifiers.
    /// </summary>
    internal void DisableIdentifierBase()
    {
        identifierBaseEnabled = false;
    }

    /// <summary>
    /// Reports whether loose.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsLoose()
    {
        return loose;
    }

    /// <summary>
    /// Reports whether strict.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsStrict()
    {
        return !loose;
    }

    /// <summary>
    /// Updates the loose setting.
    /// </summary>
    /// <param name="looseValue">Whether to enable loose value.</param>
    internal void SetLoose(bool looseValue)
    {
        loose = looseValue;
    }

    /// <summary>
    /// Reports whether prerelease candidates should participate in the increment operation.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool ShouldIncludePrerelease()
    {
        return includePrerelease;
    }

    /// <summary>
    /// Updates the include prerelease setting.
    /// </summary>
    /// <param name="includePrereleaseValue">Whether to enable include prerelease value.</param>
    internal void SetIncludePrerelease(bool includePrereleaseValue)
    {
        includePrerelease = includePrereleaseValue;
    }

    internal static void ValidateReleaseType(NodeSemVerIncrementReleaseType releaseTypeValue)
    {
        if (releaseTypeValue == NodeSemVerIncrementReleaseType.Major)
        {
            return;
        }

        if (releaseTypeValue == NodeSemVerIncrementReleaseType.Minor)
        {
            return;
        }

        if (releaseTypeValue == NodeSemVerIncrementReleaseType.Patch)
        {
            return;
        }

        if (releaseTypeValue == NodeSemVerIncrementReleaseType.Premajor)
        {
            return;
        }

        if (releaseTypeValue == NodeSemVerIncrementReleaseType.Preminor)
        {
            return;
        }

        if (releaseTypeValue == NodeSemVerIncrementReleaseType.Prepatch)
        {
            return;
        }

        if (releaseTypeValue == NodeSemVerIncrementReleaseType.Prerelease)
        {
            return;
        }

        if (releaseTypeValue == NodeSemVerIncrementReleaseType.Release)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("releaseTypeValue");
    }

    internal static void ValidateIdentifier(string identifierValue)
    {
        int index = 0;
        while (index < identifierValue.Length)
        {
            if (!VersionChar.IsAlphaNumeric(identifierValue[index]) && identifierValue[index] != '-')
            {
                throw new System.ArgumentException("Prerelease identifiers can contain only ASCII letters, digits, and hyphens.");
            }

            index = index + 1;
        }
    }
}
