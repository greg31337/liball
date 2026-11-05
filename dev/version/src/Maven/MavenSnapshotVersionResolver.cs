namespace version;

internal static class MavenSnapshotVersionResolver
{
    internal static MavenArtifactVersionIdentity? ResolveSnapshotVersion(
        MavenArtifactVersionIdentity snapshotIdentity,
        MavenRepositoryMetadata metadata,
        string extension,
        string? classifier)
    {
        System.ArgumentNullException.ThrowIfNull(snapshotIdentity);
        System.ArgumentNullException.ThrowIfNull(metadata);
        MavenCoordinateValidation.ValidateToken(extension, "Maven snapshot extension cannot be empty or contain invalid characters.");

        if (classifier != null && classifier.Length > 0)
        {
            MavenCoordinateValidation.ValidateToken(classifier, "Maven snapshot classifier cannot contain invalid characters.");
        }

        ValidateSnapshotInputs(snapshotIdentity, metadata);

        MavenSnapshotVersionMetadata? snapshotVersion = FindSnapshotVersion(metadata, extension, classifier);
        if (snapshotVersion != null)
        {
            return CreateResolvedIdentity(snapshotIdentity, extension, classifier, snapshotVersion.GetValue());
        }

        if (metadata.GetSnapshotVersionCount() > 0)
        {
            return null;
        }

        if (!metadata.HasSnapshotMetadata())
        {
            return null;
        }

        MavenSnapshotMetadata snapshotMetadata = metadata.GetSnapshotMetadata();
        if (snapshotMetadata.IsLocalCopy())
        {
            return CreateResolvedIdentity(snapshotIdentity, extension, classifier, snapshotIdentity.GetVersionText());
        }

        return CreateResolvedIdentity(
            snapshotIdentity,
            extension,
            classifier,
            CreateTimestampedVersion(snapshotIdentity.GetVersionText(), snapshotMetadata));
    }

    internal static MavenArtifactVersionIdentity? ResolveSnapshotVersion(
        string groupIdText,
        string artifactIdText,
        string baseSnapshotVersionText,
        MavenRepositoryMetadata metadata,
        string extension,
        string? classifier)
    {
        return ResolveSnapshotVersion(
            MavenArtifactVersionIdentity.Create(groupIdText, artifactIdText, extension, classifier, baseSnapshotVersionText),
            metadata,
            extension,
            classifier);
    }

    internal static MavenSnapshotVersionMetadata? FindSnapshotVersion(
        MavenRepositoryMetadata metadata,
        string extension,
        string? classifier)
    {
        int index = 0;

        while (index < metadata.GetSnapshotVersionCount())
        {
            MavenSnapshotVersionMetadata snapshotVersion = metadata.GetSnapshotVersion(index);
            if (SnapshotVersionMatches(snapshotVersion, extension, classifier))
            {
                return snapshotVersion;
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool SnapshotVersionMatches(
        MavenSnapshotVersionMetadata snapshotVersion,
        string extension,
        string? classifier)
    {
        if (snapshotVersion.GetExtension() != extension)
        {
            return false;
        }

        if (classifier == null || classifier.Length == 0)
        {
            return !snapshotVersion.HasClassifier();
        }

        return snapshotVersion.HasClassifier() && snapshotVersion.GetClassifier() == classifier;
    }

    internal static MavenArtifactVersionIdentity CreateResolvedIdentity(
        MavenArtifactVersionIdentity snapshotIdentity,
        string extension,
        string? classifier,
        string resolvedVersionText)
    {
        return MavenArtifactVersionIdentity.Create(
            snapshotIdentity.GetGroupId().GetValue(),
            snapshotIdentity.GetArtifactId().GetValue(),
            extension,
            classifier,
            resolvedVersionText);
    }

    internal static string CreateTimestampedVersion(string baseSnapshotVersionText, MavenSnapshotMetadata snapshotMetadata)
    {
        string suffix = "-snapshot";
        string basePrefix = baseSnapshotVersionText.Substring(0, baseSnapshotVersionText.Length - suffix.Length);

        return basePrefix + "-" + snapshotMetadata.GetTimestamp() + "-" + snapshotMetadata.GetBuildNumber().ToString();
    }

    internal static void ValidateSnapshotInputs(MavenArtifactVersionIdentity snapshotIdentity, MavenRepositoryMetadata metadata)
    {
        if (!EndsWithSnapshot(snapshotIdentity.GetVersionText()))
        {
            throw new System.ArgumentException("Maven snapshot version resolution requires a snapshot version identity.");
        }

        if (!metadata.GetGroupId().EqualsId(snapshotIdentity.GetGroupId()))
        {
            throw new System.ArgumentException("Maven snapshot metadata group id does not match the snapshot identity.");
        }

        if (!metadata.GetArtifactId().EqualsId(snapshotIdentity.GetArtifactId()))
        {
            throw new System.ArgumentException("Maven snapshot metadata artifact id does not match the snapshot identity.");
        }

        if (metadata.HasVersion() && MavenVersionComparer.Compare(metadata.GetVersionText(), snapshotIdentity.GetVersionText()) != 0)
        {
            throw new System.ArgumentException("Maven snapshot metadata version does not match the snapshot identity.");
        }
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
