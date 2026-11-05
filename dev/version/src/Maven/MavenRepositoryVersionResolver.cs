namespace version;

internal static class MavenRepositoryVersionResolver
{
    internal static MavenArtifactVersionIdentity? ResolveReleaseVersion(MavenRepositoryMetadata metadata)
    {
        System.ArgumentNullException.ThrowIfNull(metadata);

        if (metadata.HasReleaseVersion())
        {
            return CreateIdentity(metadata, metadata.GetReleaseVersion());
        }

        string? versionText = SelectHighestVersion(metadata, false, null);
        if (versionText == null)
        {
            return null;
        }

        return CreateIdentity(metadata, versionText);
    }

    internal static MavenArtifactVersionIdentity? ResolveLatestVersion(MavenRepositoryMetadata metadata)
    {
        System.ArgumentNullException.ThrowIfNull(metadata);

        if (metadata.HasLatestVersion())
        {
            return CreateIdentity(metadata, metadata.GetLatestVersion());
        }

        string? versionText = SelectHighestVersion(metadata, true, null);
        if (versionText == null)
        {
            return null;
        }

        return CreateIdentity(metadata, versionText);
    }

    internal static MavenArtifactVersionIdentity? ResolveHighestRangeVersion(MavenRepositoryMetadata metadata, string rangeText)
    {
        System.ArgumentNullException.ThrowIfNull(metadata);
        System.ArgumentNullException.ThrowIfNull(rangeText);

        if (!IsExplicitRangeText(rangeText))
        {
            throw new System.ArgumentException("Maven repository range resolution requires an explicit Maven interval range.");
        }

        VersionRangeParseResult result = MavenRangeParser.Parse(rangeText);
        if (result.IsFailure() || !result.HasRange())
        {
            throw new System.ArgumentException("Invalid Maven repository version range.");
        }

        string? versionText = SelectHighestVersion(metadata, true, result.GetRange());
        if (versionText == null)
        {
            return null;
        }

        return CreateIdentity(metadata, versionText);
    }

    internal static MavenArtifactVersionIdentity? ResolveSnapshotVersion(
        MavenRepositoryMetadata metadata,
        string extension,
        string? classifier)
    {
        System.ArgumentNullException.ThrowIfNull(metadata);

        if (!metadata.HasVersion())
        {
            return null;
        }

        return MavenSnapshotVersionResolver.ResolveSnapshotVersion(
            metadata.GetGroupId().GetValue(),
            metadata.GetArtifactId().GetValue(),
            metadata.GetVersionText(),
            metadata,
            extension,
            classifier);
    }

    internal static MavenArtifactVersionResolutionResult ResolveSnapshotVersionWithDiagnostics(
        MavenRepositoryMetadata metadata,
        string extension,
        string? classifier)
    {
        System.ArgumentNullException.ThrowIfNull(metadata);

        if (!metadata.HasVersion() || !IsSnapshotVersion(metadata.GetVersionText()))
        {
            return SnapshotFailure(metadata, "Maven snapshot resolution requires metadata for a snapshot version.");
        }

        MavenArtifactVersionIdentity? identity = ResolveSnapshotVersion(metadata, extension, classifier);
        if (identity == null)
        {
            return SnapshotFailure(metadata, "Maven snapshot metadata does not contain a matching snapshot artifact version.");
        }

        return MavenArtifactVersionResolutionResult.CreateSuccess(identity);
    }

    internal static string? SelectHighestVersion(MavenRepositoryMetadata metadata, bool includeSnapshots, VersionRange? range)
    {
        string? selected = null;
        int index = 0;

        while (index < metadata.GetVersionCount())
        {
            string versionText = metadata.GetVersion(index);
            if (CandidateMatches(versionText, includeSnapshots, range))
            {
                if (selected == null || MavenVersionComparer.Compare(versionText, selected) > 0)
                {
                    selected = versionText;
                }
            }

            index = index + 1;
        }

        return selected;
    }

    internal static bool CandidateMatches(string versionText, bool includeSnapshots, VersionRange? range)
    {
        if (!includeSnapshots && IsSnapshotVersion(versionText))
        {
            return false;
        }

        if (range == null)
        {
            return true;
        }

        return range.ContainsText(versionText, VersionDialect.Maven);
    }

    internal static bool IsExplicitRangeText(string rangeText)
    {
        int index = 0;

        while (index < rangeText.Length && VersionChar.IsWhitespace(rangeText[index]))
        {
            index = index + 1;
        }

        if (index >= rangeText.Length)
        {
            return false;
        }

        return rangeText[index] == '[' || rangeText[index] == '(';
    }

    internal static bool IsSnapshotVersion(string versionText)
    {
        string suffix = "-snapshot";
        string normalized = versionText.ToLowerInvariant();

        if (normalized.Length < suffix.Length)
        {
            return false;
        }

        return normalized.Substring(normalized.Length - suffix.Length) == suffix;
    }

    internal static MavenArtifactVersionResolutionResult SnapshotFailure(MavenRepositoryMetadata metadata, string message)
    {
        string sourceText = metadata.GetGroupId().GetValue() + ":" + metadata.GetArtifactId().GetValue();
        if (metadata.HasVersion())
        {
            sourceText = sourceText + ":" + metadata.GetVersionText();
        }

        return MavenArtifactVersionResolutionResult.CreateFailure(
            VersionDiagnostic.FromIndex(
                MavenMetadataDiagnosticCodes.MissingSnapshotMetadata,
                message,
                sourceText,
                0));
    }

    internal static MavenArtifactVersionIdentity CreateIdentity(MavenRepositoryMetadata metadata, string versionText)
    {
        return MavenArtifactVersionIdentity.Create(
            metadata.GetGroupId().GetValue(),
            metadata.GetArtifactId().GetValue(),
            "jar",
            null,
            versionText);
    }
}
