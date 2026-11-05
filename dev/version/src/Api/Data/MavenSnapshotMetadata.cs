namespace version;

/// <summary>
/// Represents a maven snapshot metadata.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenSnapshotMetadata
{
    /// <summary>
    /// Stores the <c>snapshotIdentity</c> value.
    /// </summary>
    public MavenArtifactVersionIdentity snapshotIdentity;
    /// <summary>
    /// Stores the <c>timestamp</c> value.
    /// </summary>
    public string timestamp;
    /// <summary>
    /// Stores the <c>buildNumber</c> value.
    /// </summary>
    public int buildNumber;
    /// <summary>
    /// Stores the <c>lastUpdated</c> value.
    /// </summary>
    public string lastUpdated;
    /// <summary>
    /// Stores the <c>localCopy</c> value.
    /// </summary>
    public bool localCopy;

    /// <summary>
    /// Initializes a new Maven Snapshot Metadata instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="snapshotIdentityValue">The snapshot identity value used by the operation.</param>
    /// <param name="timestampValue">The timestamp value used by the operation.</param>
    /// <param name="buildNumberValue">The build number value used by the operation.</param>
    /// <param name="lastUpdatedValue">The last updated value used by the operation.</param>
    /// <param name="localCopyValue">Whether to enable local copy value.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public MavenSnapshotMetadata(
        MavenArtifactVersionIdentity snapshotIdentityValue,
        string timestampValue,
        int buildNumberValue,
        string lastUpdatedValue,
        bool localCopyValue)
    {
        System.ArgumentNullException.ThrowIfNull(snapshotIdentityValue);
        System.ArgumentNullException.ThrowIfNull(timestampValue);
        System.ArgumentNullException.ThrowIfNull(lastUpdatedValue);

        if (!EndsWithSnapshot(snapshotIdentityValue.GetVersionText()))
        {
            throw new System.ArgumentException("Maven snapshot metadata requires a snapshot version identity.");
        }

        if (buildNumberValue < 0)
        {
            throw new System.ArgumentOutOfRangeException("buildNumberValue");
        }

        if (!localCopyValue && timestampValue.Length == 0)
        {
            throw new System.ArgumentException("Remote Maven snapshot metadata requires a timestamp.");
        }

        if (!localCopyValue && buildNumberValue == 0)
        {
            throw new System.ArgumentException("Remote Maven snapshot metadata requires a build number.");
        }

        snapshotIdentity = snapshotIdentityValue;
        timestamp = timestampValue;
        buildNumber = buildNumberValue;
        lastUpdated = lastUpdatedValue;
        localCopy = localCopyValue;
    }

    /// <summary>
    /// Creates a local snapshot.
    /// </summary>
    /// <param name="snapshotIdentity">The snapshot identity used by the operation.</param>
    /// <returns>The resulting maven snapshot metadata.</returns>
    internal static MavenSnapshotMetadata CreateLocalSnapshot(MavenArtifactVersionIdentity snapshotIdentity)
    {
        return new MavenSnapshotMetadata(snapshotIdentity, "", 0, "", true);
    }

    /// <summary>
    /// Creates a remote snapshot.
    /// </summary>
    /// <param name="snapshotIdentity">The snapshot identity used by the operation.</param>
    /// <param name="timestamp">The timestamp used by the operation.</param>
    /// <param name="buildNumber">The build number used by the operation.</param>
    /// <param name="lastUpdated">The last updated used by the operation.</param>
    /// <returns>The resulting maven snapshot metadata.</returns>
    internal static MavenSnapshotMetadata CreateRemoteSnapshot(
        MavenArtifactVersionIdentity snapshotIdentity,
        string timestamp,
        int buildNumber,
        string lastUpdated)
    {
        return new MavenSnapshotMetadata(snapshotIdentity, timestamp, buildNumber, lastUpdated, false);
    }

    /// <summary>
    /// Gets a snapshot identity.
    /// </summary>
    /// <returns>The resulting maven artifact version identity.</returns>
    internal MavenArtifactVersionIdentity GetSnapshotIdentity()
    {
        return snapshotIdentity;
    }

    /// <summary>
    /// Reports whether local copy.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsLocalCopy()
    {
        return localCopy;
    }

    /// <summary>
    /// Reports whether this value has timestamp.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasTimestamp()
    {
        return timestamp.Length > 0;
    }

    /// <summary>
    /// Gets a timestamp.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetTimestamp()
    {
        return timestamp;
    }

    /// <summary>
    /// Gets a build number.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetBuildNumber()
    {
        return buildNumber;
    }

    /// <summary>
    /// Reports whether this value has last updated.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasLastUpdated()
    {
        return lastUpdated.Length > 0;
    }

    /// <summary>
    /// Gets a last updated.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetLastUpdated()
    {
        return lastUpdated;
    }

    internal static bool EndsWithSnapshot(string versionText)
    {
        string suffix = "-snapshot";
        string normalized = versionText.ToLowerInvariant();

        if (suffix.Length > normalized.Length)
        {
            return false;
        }

        return normalized.Substring(normalized.Length - suffix.Length) == suffix;
    }
}
