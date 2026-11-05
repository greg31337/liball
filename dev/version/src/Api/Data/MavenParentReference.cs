namespace version;

/// <summary>
/// Represents a maven parent reference.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenParentReference
{
    /// <summary>
    /// Stores the <c>identity</c> value.
    /// </summary>
    public MavenArtifactVersionIdentity identity;
    /// <summary>
    /// Stores the <c>relativePath</c> value.
    /// </summary>
    public string relativePath;
    /// <summary>
    /// Stores the <c>relativePathSpecified</c> value.
    /// </summary>
    public bool relativePathSpecified;

    /// <summary>
    /// Initializes a new Maven Parent Reference instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="identityValue">The identity value used by the operation.</param>
    /// <param name="relativePathValue">The relative path value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public MavenParentReference(MavenArtifactVersionIdentity identityValue, string? relativePathValue)
    {
        System.ArgumentNullException.ThrowIfNull(identityValue);

        if (identityValue.GetPackaging() != "pom" || identityValue.HasClassifier())
        {
            throw new System.ArgumentException("Maven parent references require an unclassified pom identity.");
        }

        identity = identityValue;

        if (relativePathValue == null)
        {
            relativePath = "../pom.xml";
            relativePathSpecified = false;
        }
        else
        {
            relativePath = relativePathValue;
            relativePathSpecified = true;
        }
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="groupIdText">The group id text to process.</param>
    /// <param name="artifactIdText">The artifact id text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <returns>The resulting maven parent reference.</returns>
    internal static MavenParentReference Create(string groupIdText, string artifactIdText, string versionText)
    {
        return new MavenParentReference(MavenArtifactVersionIdentity.Create(groupIdText, artifactIdText, "pom", null, versionText), null);
    }

    /// <summary>
    /// Creates a with relative path.
    /// </summary>
    /// <param name="groupIdText">The group id text to process.</param>
    /// <param name="artifactIdText">The artifact id text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <param name="relativePath">The relative path used by the operation.</param>
    /// <returns>The resulting maven parent reference.</returns>
    internal static MavenParentReference CreateWithRelativePath(
        string groupIdText,
        string artifactIdText,
        string versionText,
        string relativePath)
    {
        return new MavenParentReference(
            MavenArtifactVersionIdentity.Create(groupIdText, artifactIdText, "pom", null, versionText),
            relativePath);
    }

    /// <summary>
    /// Gets an identity.
    /// </summary>
    /// <returns>The resulting maven artifact version identity.</returns>
    internal MavenArtifactVersionIdentity GetIdentity()
    {
        return identity;
    }

    /// <summary>
    /// Gets a group id.
    /// </summary>
    /// <returns>The resulting maven group id.</returns>
    internal MavenGroupId GetGroupId()
    {
        return identity.GetGroupId();
    }

    /// <summary>
    /// Gets an artifact id.
    /// </summary>
    /// <returns>The resulting maven artifact id.</returns>
    internal MavenArtifactId GetArtifactId()
    {
        return identity.GetArtifactId();
    }

    /// <summary>
    /// Gets a version text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetVersionText()
    {
        return identity.GetVersionText();
    }

    /// <summary>
    /// Reports whether this value has explicit relative path.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasExplicitRelativePath()
    {
        return relativePathSpecified;
    }

    /// <summary>
    /// Gets a relative path.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetRelativePath()
    {
        return relativePath;
    }
}
