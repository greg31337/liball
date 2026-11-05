namespace version;

/// <summary>
/// Represents a maven artifact metadata.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenArtifactMetadata
{
    /// <summary>
    /// Stores the <c>identity</c> value.
    /// </summary>
    public MavenArtifactVersionIdentity identity;
    /// <summary>
    /// Stores the <c>repositoryId</c> value.
    /// </summary>
    public MavenRepositoryId repositoryId;
    /// <summary>
    /// Stores the <c>snapshotMetadata</c> value.
    /// </summary>
    public MavenSnapshotMetadata? snapshotMetadata;

    /// <summary>
    /// Initializes a new Maven Artifact Metadata instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="identityValue">The identity value used by the operation.</param>
    /// <param name="repositoryIdValue">The repository id value used by the operation.</param>
    /// <param name="snapshotMetadataValue">The snapshot metadata value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public MavenArtifactMetadata(
        MavenArtifactVersionIdentity identityValue,
        MavenRepositoryId repositoryIdValue,
        MavenSnapshotMetadata? snapshotMetadataValue)
    {
        System.ArgumentNullException.ThrowIfNull(identityValue);
        System.ArgumentNullException.ThrowIfNull(repositoryIdValue);

        if (snapshotMetadataValue != null && !snapshotMetadataValue.GetSnapshotIdentity().HasSameIdentity(identityValue))
        {
            throw new System.ArgumentException("Maven artifact metadata snapshot details must match the artifact identity.");
        }

        identity = identityValue;
        repositoryId = repositoryIdValue;
        snapshotMetadata = snapshotMetadataValue;
    }

    /// <summary>
    /// Creates a release.
    /// </summary>
    /// <param name="identity">The identity used by the operation.</param>
    /// <param name="repositoryId">The repository id used by the operation.</param>
    /// <returns>The resulting maven artifact metadata.</returns>
    internal static MavenArtifactMetadata CreateRelease(
        MavenArtifactVersionIdentity identity,
        MavenRepositoryId repositoryId)
    {
        return new MavenArtifactMetadata(identity, repositoryId, null);
    }

    /// <summary>
    /// Creates a snapshot.
    /// </summary>
    /// <param name="snapshotMetadata">The snapshot metadata used by the operation.</param>
    /// <param name="repositoryId">The repository id used by the operation.</param>
    /// <returns>The resulting maven artifact metadata.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal static MavenArtifactMetadata CreateSnapshot(
        MavenSnapshotMetadata snapshotMetadata,
        MavenRepositoryId repositoryId)
    {
        System.ArgumentNullException.ThrowIfNull(snapshotMetadata);

        return new MavenArtifactMetadata(snapshotMetadata.GetSnapshotIdentity(), repositoryId, snapshotMetadata);
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
    /// Gets a repository id.
    /// </summary>
    /// <returns>The resulting maven repository id.</returns>
    internal MavenRepositoryId GetRepositoryId()
    {
        return repositoryId;
    }

    /// <summary>
    /// Reports whether snapshot.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsSnapshot()
    {
        return snapshotMetadata != null;
    }

    /// <summary>
    /// Reports whether this value has snapshot metadata.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasSnapshotMetadata()
    {
        return snapshotMetadata != null;
    }

    /// <summary>
    /// Gets a snapshot metadata.
    /// </summary>
    /// <returns>The resulting maven snapshot metadata.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal MavenSnapshotMetadata GetSnapshotMetadata()
    {
        if (snapshotMetadata == null)
        {
            throw new System.InvalidOperationException("Maven artifact metadata does not contain snapshot details.");
        }

        return snapshotMetadata;
    }
}
