namespace version;

/// <summary>
/// Represents a maven repository metadata.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenRepositoryMetadata
{
    /// <summary>
    /// Stores the <c>groupId</c> value.
    /// </summary>
    public MavenGroupId groupId;
    /// <summary>
    /// Stores the <c>artifactId</c> value.
    /// </summary>
    public MavenArtifactId artifactId;
    /// <summary>
    /// Stores the <c>versionText</c> value.
    /// </summary>
    public string versionText;
    /// <summary>
    /// Stores the <c>versionSpecified</c> value.
    /// </summary>
    public bool versionSpecified;
    /// <summary>
    /// Stores the <c>latestVersion</c> value.
    /// </summary>
    public string latestVersion;
    /// <summary>
    /// Stores the <c>releaseVersion</c> value.
    /// </summary>
    public string releaseVersion;
    /// <summary>
    /// Stores the <c>versions</c> value.
    /// </summary>
    public string[] versions;
    /// <summary>
    /// Stores the <c>lastUpdated</c> value.
    /// </summary>
    public string lastUpdated;
    /// <summary>
    /// Stores the <c>snapshotMetadata</c> value.
    /// </summary>
    public MavenSnapshotMetadata? snapshotMetadata;
    /// <summary>
    /// Stores the <c>snapshotVersions</c> value.
    /// </summary>
    public MavenSnapshotVersionMetadata[] snapshotVersions;

    /// <summary>
    /// Initializes a new Maven Repository Metadata instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="groupIdValue">The group id value used by the operation.</param>
    /// <param name="artifactIdValue">The artifact id value used by the operation.</param>
    /// <param name="versionTextValue">The version text value to process.</param>
    /// <param name="latestVersionValue">The latest version value used by the operation.</param>
    /// <param name="releaseVersionValue">The release version value used by the operation.</param>
    /// <param name="versionValues">The version values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="lastUpdatedValue">The last updated value used by the operation.</param>
    /// <param name="snapshotMetadataValue">The snapshot metadata value used by the operation.</param>
    /// <param name="snapshotVersionValues">The snapshot version values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public MavenRepositoryMetadata(
        MavenGroupId groupIdValue,
        MavenArtifactId artifactIdValue,
        string? versionTextValue,
        string latestVersionValue,
        string releaseVersionValue,
        string[] versionValues,
        string lastUpdatedValue,
        MavenSnapshotMetadata? snapshotMetadataValue,
        MavenSnapshotVersionMetadata[] snapshotVersionValues)
    {
        System.ArgumentNullException.ThrowIfNull(groupIdValue);
        System.ArgumentNullException.ThrowIfNull(artifactIdValue);
        System.ArgumentNullException.ThrowIfNull(latestVersionValue);
        System.ArgumentNullException.ThrowIfNull(releaseVersionValue);
        System.ArgumentNullException.ThrowIfNull(versionValues);
        System.ArgumentNullException.ThrowIfNull(lastUpdatedValue);
        System.ArgumentNullException.ThrowIfNull(snapshotVersionValues);
        ValidateOptionalVersion(latestVersionValue, "Invalid Maven latest version.");
        ValidateOptionalVersion(releaseVersionValue, "Invalid Maven release version.");

        groupId = groupIdValue;
        artifactId = artifactIdValue;
        latestVersion = latestVersionValue;
        releaseVersion = releaseVersionValue;
        versions = CopyVersions(versionValues);
        lastUpdated = lastUpdatedValue;
        snapshotMetadata = snapshotMetadataValue;
        snapshotVersions = CopySnapshotVersions(snapshotVersionValues);

        if (versionTextValue == null || versionTextValue.Length == 0)
        {
            versionText = "";
            versionSpecified = false;
        }
        else
        {
            ValidateOptionalVersion(versionTextValue, "Invalid Maven metadata version.");
            versionText = versionTextValue;
            versionSpecified = true;
        }

        ValidateSnapshotIdentity();
    }

    /// <summary>
    /// Gets a group id.
    /// </summary>
    /// <returns>The resulting maven group id.</returns>
    internal MavenGroupId GetGroupId()
    {
        return groupId;
    }

    /// <summary>
    /// Gets an artifact id.
    /// </summary>
    /// <returns>The resulting maven artifact id.</returns>
    internal MavenArtifactId GetArtifactId()
    {
        return artifactId;
    }

    /// <summary>
    /// Reports whether this value has version.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasVersion()
    {
        return versionSpecified;
    }

    /// <summary>
    /// Gets a version text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetVersionText()
    {
        return versionText;
    }

    /// <summary>
    /// Reports whether this value has latest version.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasLatestVersion()
    {
        return latestVersion.Length > 0;
    }

    /// <summary>
    /// Gets a latest version.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetLatestVersion()
    {
        return latestVersion;
    }

    /// <summary>
    /// Reports whether this value has release version.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasReleaseVersion()
    {
        return releaseVersion.Length > 0;
    }

    /// <summary>
    /// Gets a release version.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetReleaseVersion()
    {
        return releaseVersion;
    }

    /// <summary>
    /// Gets a version count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetVersionCount()
    {
        return versions.Length;
    }

    /// <summary>
    /// Gets a version.
    /// </summary>
    /// <param name="versionIndex">The zero-based version index.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal string GetVersion(int versionIndex)
    {
        if (versionIndex < 0 || versionIndex >= versions.Length)
        {
            throw new System.ArgumentOutOfRangeException("versionIndex");
        }

        return versions[versionIndex];
    }

    /// <summary>
    /// Gets a versions.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal string[] GetVersions()
    {
        return CopyStrings(versions);
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
            throw new System.InvalidOperationException("Maven repository metadata does not contain snapshot details.");
        }

        return snapshotMetadata;
    }

    /// <summary>
    /// Gets a snapshot version count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetSnapshotVersionCount()
    {
        return snapshotVersions.Length;
    }

    /// <summary>
    /// Gets a snapshot version.
    /// </summary>
    /// <param name="snapshotVersionIndex">The zero-based snapshot version index.</param>
    /// <returns>The resulting maven snapshot version metadata.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal MavenSnapshotVersionMetadata GetSnapshotVersion(int snapshotVersionIndex)
    {
        if (snapshotVersionIndex < 0 || snapshotVersionIndex >= snapshotVersions.Length)
        {
            throw new System.ArgumentOutOfRangeException("snapshotVersionIndex");
        }

        return snapshotVersions[snapshotVersionIndex];
    }

    /// <summary>
    /// Gets a snapshot versions.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal MavenSnapshotVersionMetadata[] GetSnapshotVersions()
    {
        return CopySnapshotVersions(snapshotVersions);
    }

    internal void ValidateSnapshotIdentity()
    {
        if (snapshotMetadata == null)
        {
            return;
        }

        if (!snapshotMetadata.GetSnapshotIdentity().GetGroupId().EqualsId(groupId))
        {
            throw new System.ArgumentException("Maven snapshot metadata group id does not match repository metadata.");
        }

        if (!snapshotMetadata.GetSnapshotIdentity().GetArtifactId().EqualsId(artifactId))
        {
            throw new System.ArgumentException("Maven snapshot metadata artifact id does not match repository metadata.");
        }

        if (versionSpecified && MavenVersionComparer.Compare(versionText, snapshotMetadata.GetSnapshotIdentity().GetVersionText()) != 0)
        {
            throw new System.ArgumentException("Maven snapshot metadata version does not match repository metadata.");
        }
    }

    internal static string[] CopyVersions(string[] versionValues)
    {
        string[] copy = new string[versionValues.Length];
        int index = 0;

        while (index < versionValues.Length)
        {
            if (versionValues[index] == null)
            {
                throw new System.ArgumentException("Maven metadata version arrays cannot contain null values.");
            }

            ValidateOptionalVersion(versionValues[index], "Invalid Maven metadata version list entry.");
            copy[index] = versionValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static string[] CopyStrings(string[] values)
    {
        string[] copy = new string[values.Length];
        int index = 0;

        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static MavenSnapshotVersionMetadata[] CopySnapshotVersions(MavenSnapshotVersionMetadata[] snapshotVersionValues)
    {
        MavenSnapshotVersionMetadata[] copy = new MavenSnapshotVersionMetadata[snapshotVersionValues.Length];
        int index = 0;

        while (index < snapshotVersionValues.Length)
        {
            if (snapshotVersionValues[index] == null)
            {
                throw new System.ArgumentException("Maven snapshot version arrays cannot contain null values.");
            }

            copy[index] = snapshotVersionValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static void ValidateOptionalVersion(string versionTextValue, string message)
    {
        if (versionTextValue.Length == 0)
        {
            return;
        }

        VersionParseResult result = MavenVersionParser.Parse(versionTextValue);
        if (result.IsFailure())
        {
            throw new System.ArgumentException(message);
        }
    }
}
