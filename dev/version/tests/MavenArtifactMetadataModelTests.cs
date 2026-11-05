using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenArtifactMetadataModelTests
{
    [Test]
    public void DependencyScope_ExposesMavenScopes()
    {
        Assert.That(Enum.GetNames(typeof(MavenDependencyScope)), Is.EqualTo(new string[]
        {
            "Compile",
            "Provided",
            "Runtime",
            "Test",
            "System",
            "Import"
        }));
    }

    [Test]
    public void MavenIds_PreserveCaseSensitiveValuesAndValidateText()
    {
        MavenGroupId groupId = new MavenGroupId("Com.Example.Tools");
        MavenArtifactId artifactId = new MavenArtifactId("demo-plugin");
        MavenRepositoryId repositoryId = new MavenRepositoryId("central");

        Assert.That(groupId.GetValue(), Is.EqualTo("Com.Example.Tools"));
        Assert.That(groupId.EqualsId(new MavenGroupId("Com.Example.Tools")), Is.True);
        Assert.That(groupId.MatchesText("com.example.tools"), Is.False);
        Assert.That(artifactId.GetValue(), Is.EqualTo("demo-plugin"));
        Assert.That(repositoryId.MatchesText("central"), Is.True);

        AssertInvalidIds();
    }

    [Test]
    public void ArtifactCoordinate_CarriesPackagingClassifierAndCoordinateText()
    {
        MavenArtifactCoordinate jar = MavenArtifactCoordinate.CreateJar("com.example", "demo");
        MavenArtifactCoordinate tests = MavenArtifactCoordinate.Create("com.example", "demo", "test-jar", "tests");
        MavenArtifactCoordinate testsAgain = new MavenArtifactCoordinate(
            new MavenGroupId("com.example"),
            new MavenArtifactId("demo"),
            "test-jar",
            "tests");

        Assert.That(jar.GetPackaging(), Is.EqualTo("jar"));
        Assert.That(jar.HasClassifier(), Is.False);
        Assert.That(jar.GetCoordinateText(), Is.EqualTo("com.example:demo:jar"));
        Assert.That(tests.HasClassifier(), Is.True);
        Assert.That(tests.GetClassifier(), Is.EqualTo("tests"));
        Assert.That(tests.GetCoordinateText(), Is.EqualTo("com.example:demo:test-jar:tests"));
        Assert.That(tests.HasSameCoordinate(testsAgain), Is.True);
        Assert.That(tests.HasSameCoordinate(jar), Is.False);
    }

    [Test]
    public void ArtifactVersionIdentity_RequiresMavenVersionAndComparesByMavenRules()
    {
        MavenArtifactVersionIdentity left = MavenArtifactVersionIdentity.Create("com.example", "demo", "1.0");
        MavenArtifactVersionIdentity right = MavenArtifactVersionIdentity.Create("com.example", "demo", "1");
        MavenArtifactVersionIdentity classified = MavenArtifactVersionIdentity.Create(
            "com.example",
            "demo",
            "jar",
            "sources",
            "1.0");

        Assert.That(left.GetGroupId().MatchesText("com.example"), Is.True);
        Assert.That(left.GetArtifactId().MatchesText("demo"), Is.True);
        Assert.That(left.GetPackaging(), Is.EqualTo("jar"));
        Assert.That(left.GetVersionText(), Is.EqualTo("1.0"));
        Assert.That(left.GetIdentityText(), Is.EqualTo("com.example:demo:jar:1.0"));
        Assert.That(left.HasSameIdentity(right), Is.True);
        Assert.That(classified.GetIdentityText(), Is.EqualTo("com.example:demo:jar:sources:1.0"));
        Assert.That(left.HasSameIdentity(classified), Is.False);

        Assert.That(
            delegate
            {
                MavenArtifactVersionIdentity.Create("com.example", "demo", "bad version");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                new MavenArtifactVersionIdentity(
                    MavenArtifactCoordinate.CreateJar("com.example", "demo"),
                    SemVerParser.ParseStrict("1.0.0").GetVersion());
            },
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void SnapshotMetadata_CarriesLocalAndRemoteSnapshotDetails()
    {
        MavenArtifactVersionIdentity snapshotIdentity = MavenArtifactVersionIdentity.Create(
            "com.example",
            "demo",
            "1.0-SNAPSHOT");
        MavenSnapshotMetadata local = MavenSnapshotMetadata.CreateLocalSnapshot(snapshotIdentity);
        MavenSnapshotMetadata remote = MavenSnapshotMetadata.CreateRemoteSnapshot(
            snapshotIdentity,
            "20250101.010203",
            4,
            "20250101010203");

        Assert.That(local.IsLocalCopy(), Is.True);
        Assert.That(local.HasTimestamp(), Is.False);
        Assert.That(remote.IsLocalCopy(), Is.False);
        Assert.That(remote.GetTimestamp(), Is.EqualTo("20250101.010203"));
        Assert.That(remote.GetBuildNumber(), Is.EqualTo(4));
        Assert.That(remote.HasLastUpdated(), Is.True);
        Assert.That(remote.GetLastUpdated(), Is.EqualTo("20250101010203"));
        Assert.That(remote.GetSnapshotIdentity().HasSameIdentity(snapshotIdentity), Is.True);
    }

    [Test]
    public void ArtifactMetadata_CarriesRepositoryAndOptionalSnapshotMetadata()
    {
        MavenRepositoryId repositoryId = new MavenRepositoryId("central");
        MavenArtifactVersionIdentity releaseIdentity = MavenArtifactVersionIdentity.Create("com.example", "demo", "1.0.0");
        MavenArtifactVersionIdentity snapshotIdentity = MavenArtifactVersionIdentity.Create("com.example", "demo", "1.0-SNAPSHOT");
        MavenSnapshotMetadata snapshot = MavenSnapshotMetadata.CreateRemoteSnapshot(
            snapshotIdentity,
            "20250101.010203",
            4,
            "20250101010203");
        MavenArtifactMetadata release = MavenArtifactMetadata.CreateRelease(releaseIdentity, repositoryId);
        MavenArtifactMetadata snapshotMetadata = MavenArtifactMetadata.CreateSnapshot(snapshot, repositoryId);

        Assert.That(release.GetIdentity().HasSameIdentity(releaseIdentity), Is.True);
        Assert.That(release.GetRepositoryId().EqualsId(repositoryId), Is.True);
        Assert.That(release.IsSnapshot(), Is.False);
        Assert.That(release.HasSnapshotMetadata(), Is.False);
        Assert.That(snapshotMetadata.IsSnapshot(), Is.True);
        Assert.That(snapshotMetadata.GetSnapshotMetadata().GetBuildNumber(), Is.EqualTo(4));

        Assert.That(
            delegate
            {
                release.GetSnapshotMetadata();
            },
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void SnapshotAndArtifactMetadata_RejectMismatchedOrInvalidSnapshotDetails()
    {
        MavenArtifactVersionIdentity releaseIdentity = MavenArtifactVersionIdentity.Create("com.example", "demo", "1.0.0");
        MavenArtifactVersionIdentity snapshotIdentity = MavenArtifactVersionIdentity.Create("com.example", "demo", "1.0-SNAPSHOT");
        MavenSnapshotMetadata snapshot = MavenSnapshotMetadata.CreateRemoteSnapshot(
            snapshotIdentity,
            "20250101.010203",
            4,
            "");

        Assert.That(
            delegate
            {
                MavenSnapshotMetadata.CreateLocalSnapshot(releaseIdentity);
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                MavenSnapshotMetadata.CreateRemoteSnapshot(snapshotIdentity, "", 1, "");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                MavenSnapshotMetadata.CreateRemoteSnapshot(snapshotIdentity, "20250101.010203", 0, "");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                new MavenArtifactMetadata(
                    MavenArtifactVersionIdentity.Create("com.example", "other", "1.0-SNAPSHOT"),
                    new MavenRepositoryId("central"),
                    snapshot);
            },
            Throws.TypeOf<ArgumentException>());
    }

    static void AssertInvalidIds()
    {
        Assert.That(
            delegate
            {
                new MavenGroupId("");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                new MavenGroupId("bad/group");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                new MavenArtifactId("bad artifact");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                new MavenRepositoryId("bad:repo");
            },
            Throws.TypeOf<ArgumentException>());
    }
}
