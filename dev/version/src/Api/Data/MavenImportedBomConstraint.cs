namespace version;

/// <summary>
/// Represents a maven imported bom constraint.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenImportedBomConstraint
{
    /// <summary>
    /// Stores the <c>identity</c> value.
    /// </summary>
    public MavenArtifactVersionIdentity identity;

    /// <summary>
    /// Initializes a new Maven Imported Bom Constraint instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="identityValue">The identity value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public MavenImportedBomConstraint(MavenArtifactVersionIdentity identityValue)
    {
        System.ArgumentNullException.ThrowIfNull(identityValue);

        if (identityValue.GetPackaging() != "pom" || identityValue.HasClassifier())
        {
            throw new System.ArgumentException("Maven imported BOM constraints require an unclassified pom identity.");
        }

        identity = identityValue;
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="groupIdText">The group id text to process.</param>
    /// <param name="artifactIdText">The artifact id text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <returns>The resulting maven imported bom constraint.</returns>
    internal static MavenImportedBomConstraint Create(string groupIdText, string artifactIdText, string versionText)
    {
        return new MavenImportedBomConstraint(
            MavenArtifactVersionIdentity.Create(groupIdText, artifactIdText, "pom", null, versionText));
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
    /// Reports whether this value has same bom.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool HasSameBom(MavenImportedBomConstraint other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return identity.HasSameIdentity(other.GetIdentity());
    }
}
