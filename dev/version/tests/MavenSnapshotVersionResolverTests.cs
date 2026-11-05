using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenSnapshotVersionResolverTests
{
    [Test]
    public void ResolveSnapshotVersion_UsesMatchingUnclassifiedSnapshotVersionEntry()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateSnapshotMetadataWithEntries());

        MavenArtifactVersionIdentity? resolved = MavenVersions.ResolveSnapshotVersion(
            "com.example",
            "demo",
            "1.0-SNAPSHOT",
            metadata,
            "jar",
            null);

        AssertResolved(resolved, "com.example:demo:jar:1.0-20250101.010203-7", false, "");
    }

    [Test]
    public void ResolveSnapshotVersion_UsesMatchingClassifierAndExtensionEntry()
    {
        MavenRepositoryMetadata metadata = MavenMetadataParser.Parse(CreateSnapshotMetadataWithEntries());
        MavenArtifactVersionIdentity snapshotIdentity = MavenArtifactVersionIdentity.Create(
            "com.example",
            "demo",
            "jar",
            "sources",
            "1.0-SNAPSHOT");

        MavenArtifactVersionIdentity? resolved = MavenSnapshotVersionResolver.ResolveSnapshotVersion(
            snapshotIdentity,
            metadata,
            "jar",
            "sources");

        AssertResolved(resolved, "com.example:demo:jar:sources:1.0-20250101.010203-7", true, "sources");
    }

    [Test]
    public void ResolveSnapshotVersion_ReturnsNullWhenSnapshotVersionEntriesExistButNoneMatch()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateSnapshotMetadataWithEntries());

        Assert.That(
            MavenVersions.ResolveSnapshotVersion("com.example", "demo", "1.0-SNAPSHOT", metadata, "pom", null),
            Is.Null);
        Assert.That(
            MavenVersions.ResolveSnapshotVersion("com.example", "demo", "1.0-SNAPSHOT", metadata, "jar", "javadoc"),
            Is.Null);
    }

    [Test]
    public void ResolveSnapshotVersion_FallsBackToTimestampAndBuildNumberWhenEntriesAreAbsent()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateSnapshotMetadataWithoutEntries());

        MavenArtifactVersionIdentity? resolved = MavenVersions.ResolveSnapshotVersion(
            "com.example",
            "demo",
            "1.0-SNAPSHOT",
            metadata,
            "war",
            "classes");

        AssertResolved(resolved, "com.example:demo:war:classes:1.0-20250101.010203-7", true, "classes");
    }

    [Test]
    public void ResolveSnapshotVersion_ReturnsBaseSnapshotVersionForLocalCopyMetadata()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateLocalSnapshotMetadata());

        MavenArtifactVersionIdentity? resolved = MavenVersions.ResolveSnapshotVersion(
            "com.example",
            "demo",
            "1.0-SNAPSHOT",
            metadata,
            "jar",
            null);

        AssertResolved(resolved, "com.example:demo:jar:1.0-SNAPSHOT", false, "");
    }

    [Test]
    public void ResolveSnapshotVersion_RejectsNonSnapshotAndMismatchedMetadataInputs()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateSnapshotMetadataWithoutEntries());

        Assert.That(
            delegate
            {
                MavenVersions.ResolveSnapshotVersion("com.example", "demo", "1.0.0", metadata, "jar", null);
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                MavenVersions.ResolveSnapshotVersion("com.example", "other", "1.0-SNAPSHOT", metadata, "jar", null);
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                MavenVersions.ResolveSnapshotVersion("com.example", "demo", "1.0-SNAPSHOT", metadata, "", null);
            },
            Throws.TypeOf<ArgumentException>());
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
        Assert.That(resolved.HasClassifier(), Is.EqualTo(hasClassifier));
        Assert.That(resolved.GetClassifier(), Is.EqualTo(classifier));
    }

    static string CreateSnapshotMetadataWithEntries()
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

    static string CreateSnapshotMetadataWithoutEntries()
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
              </versioning>
            </metadata>
            """;
    }

    static string CreateLocalSnapshotMetadata()
    {
        return """
            <metadata>
              <groupId>com.example</groupId>
              <artifactId>demo</artifactId>
              <version>1.0-SNAPSHOT</version>
              <versioning>
                <snapshot>
                  <localCopy>true</localCopy>
                </snapshot>
              </versioning>
            </metadata>
            """;
    }
}
