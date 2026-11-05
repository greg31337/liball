using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenRepositoryVersionResolverTests
{
    [Test]
    public void ResolveReleaseVersion_UsesReleaseFieldBeforeVersionListFallback()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateReleaseMetadata());

        MavenArtifactVersionIdentity? resolved = MavenVersions.ResolveReleaseVersion(metadata);

        AssertResolved(resolved, "com.example:demo:jar:1.1.0", false, "");
    }

    [Test]
    public void ResolveReleaseVersion_FallsBackToHighestNonSnapshotVersion()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateVersionsOnlyMetadata());

        MavenArtifactVersionIdentity? resolved = MavenRepositoryVersionResolver.ResolveReleaseVersion(metadata);

        AssertResolved(resolved, "com.example:demo:jar:2.0.0", false, "");
    }

    [Test]
    public void ResolveLatestVersion_UsesLatestFieldBeforeVersionListFallback()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateReleaseMetadata());

        MavenArtifactVersionIdentity? resolved = MavenVersions.ResolveLatestVersion(metadata);

        AssertResolved(resolved, "com.example:demo:jar:3.0.0-SNAPSHOT", false, "");
    }

    [Test]
    public void ResolveLatestVersion_FallsBackToHighestVersionIncludingSnapshots()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateVersionsOnlyMetadata());

        MavenArtifactVersionIdentity? resolved = MavenVersions.ResolveLatestVersion(metadata);

        AssertResolved(resolved, "com.example:demo:jar:3.0.0-SNAPSHOT", false, "");
    }

    [Test]
    public void ResolveHighestRangeVersion_SelectsHighestVersionInExplicitRange()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateVersionsOnlyMetadata());

        MavenArtifactVersionIdentity? resolved = MavenVersions.ResolveHighestRangeVersion(metadata, "[1.0.0,2.0.0)");

        AssertResolved(resolved, "com.example:demo:jar:1.5.0", false, "");
        Assert.That(MavenVersions.ResolveHighestRangeVersion(metadata, "[4.0.0,)"), Is.Null);
    }

    [Test]
    public void ResolveHighestRangeVersion_RejectsSoftOrInvalidRangeText()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateVersionsOnlyMetadata());

        Assert.That(
            delegate
            {
                MavenVersions.ResolveHighestRangeVersion(metadata, "1.0.0");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                MavenVersions.ResolveHighestRangeVersion(metadata, "[bad version]");
            },
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void ResolveSnapshotVersion_UsesMetadataVersionAndSnapshotEntries()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateSnapshotMetadata());

        MavenArtifactVersionIdentity? resolved = MavenVersions.ResolveSnapshotVersion(metadata, "jar", "sources");

        AssertResolved(resolved, "com.example:demo:jar:sources:1.0-20250101.010203-7", true, "sources");
    }

    [Test]
    public void ResolveRepositoryVersions_ReturnNullWhenMetadataHasNoSelectableVersions()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateEmptyVersioningMetadata());

        Assert.That(MavenVersions.ResolveReleaseVersion(metadata), Is.Null);
        Assert.That(MavenVersions.ResolveLatestVersion(metadata), Is.Null);
        Assert.That(MavenVersions.ResolveHighestRangeVersion(metadata, "[1.0.0,)"), Is.Null);
        Assert.That(MavenVersions.ResolveSnapshotVersion(metadata, "jar", null), Is.Null);
    }

    static void AssertResolved(
        MavenArtifactVersionIdentity? resolved,
        string identityText,
        bool hasClassifier,
        string classifier)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved!.GetIdentityText(), Is.EqualTo(identityText));
        Assert.That(resolved.GetPackaging(), Is.EqualTo("jar"));
        Assert.That(resolved.HasClassifier(), Is.EqualTo(hasClassifier));
        Assert.That(resolved.GetClassifier(), Is.EqualTo(classifier));
    }

    static string CreateReleaseMetadata()
    {
        return """
            <metadata>
              <groupId>com.example</groupId>
              <artifactId>demo</artifactId>
              <versioning>
                <latest>3.0.0-SNAPSHOT</latest>
                <release>1.1.0</release>
                <versions>
                  <version>1.0.0</version>
                  <version>1.1.0</version>
                  <version>2.0.0</version>
                  <version>3.0.0-SNAPSHOT</version>
                </versions>
              </versioning>
            </metadata>
            """;
    }

    static string CreateVersionsOnlyMetadata()
    {
        return """
            <metadata>
              <groupId>com.example</groupId>
              <artifactId>demo</artifactId>
              <versioning>
                <versions>
                  <version>1.0.0</version>
                  <version>1.5.0</version>
                  <version>2.0.0</version>
                  <version>3.0.0-SNAPSHOT</version>
                </versions>
              </versioning>
            </metadata>
            """;
    }

    static string CreateSnapshotMetadata()
    {
        return """
            <metadata>
              <groupId>com.example</groupId>
              <artifactId>demo</artifactId>
              <version>1.0-SNAPSHOT</version>
              <versioning>
                <snapshot>
                  <timestamp>20250101.010203</timestamp>
                  <buildNumber>7</buildNumber>
                </snapshot>
                <snapshotVersions>
                  <snapshotVersion>
                    <extension>jar</extension>
                    <value>1.0-20250101.010203-7</value>
                  </snapshotVersion>
                  <snapshotVersion>
                    <extension>jar</extension>
                    <classifier>sources</classifier>
                    <value>1.0-20250101.010203-7</value>
                  </snapshotVersion>
                </snapshotVersions>
              </versioning>
            </metadata>
            """;
    }

    static string CreateEmptyVersioningMetadata()
    {
        return """
            <metadata>
              <groupId>com.example</groupId>
              <artifactId>demo</artifactId>
              <versioning>
              </versioning>
            </metadata>
            """;
    }
}
