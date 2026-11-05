using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenMetadataParserTests
{
    [Test]
    public void Parse_ReadsReleaseLatestVersionListAndLastUpdated()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateReleaseMetadata());

        Assert.That(metadata.GetGroupId().MatchesText("com.example"), Is.True);
        Assert.That(metadata.GetArtifactId().MatchesText("demo"), Is.True);
        Assert.That(metadata.HasVersion(), Is.False);
        Assert.That(metadata.GetLatestVersion(), Is.EqualTo("1.2.0"));
        Assert.That(metadata.GetReleaseVersion(), Is.EqualTo("1.1.0"));
        Assert.That(metadata.GetVersionCount(), Is.EqualTo(3));
        Assert.That(metadata.GetVersion(0), Is.EqualTo("1.0.0"));
        Assert.That(metadata.GetVersion(1), Is.EqualTo("1.1.0"));
        Assert.That(metadata.GetVersion(2), Is.EqualTo("1.2.0"));
        Assert.That(metadata.GetLastUpdated(), Is.EqualTo("20250101010203"));
        Assert.That(metadata.HasSnapshotMetadata(), Is.False);
        Assert.That(metadata.GetSnapshotVersionCount(), Is.EqualTo(0));
    }

    [Test]
    public void Parse_ReadsRemoteSnapshotMetadataAndSnapshotVersionEntries()
    {
        MavenRepositoryMetadata metadata = MavenMetadataParser.Parse(CreateSnapshotMetadata());
        MavenSnapshotMetadata snapshot = metadata.GetSnapshotMetadata();
        MavenSnapshotVersionMetadata binary = metadata.GetSnapshotVersion(0);
        MavenSnapshotVersionMetadata sources = metadata.GetSnapshotVersion(1);

        Assert.That(metadata.HasVersion(), Is.True);
        Assert.That(metadata.GetVersionText(), Is.EqualTo("1.0-SNAPSHOT"));
        Assert.That(snapshot.IsLocalCopy(), Is.False);
        Assert.That(snapshot.GetTimestamp(), Is.EqualTo("20250101.010203"));
        Assert.That(snapshot.GetBuildNumber(), Is.EqualTo(7));
        Assert.That(snapshot.GetLastUpdated(), Is.EqualTo("20250101010203"));
        Assert.That(binary.GetExtension(), Is.EqualTo("jar"));
        Assert.That(binary.HasClassifier(), Is.False);
        Assert.That(binary.GetValue(), Is.EqualTo("1.0-20250101.010203-7"));
        Assert.That(sources.GetExtension(), Is.EqualTo("jar"));
        Assert.That(sources.HasClassifier(), Is.True);
        Assert.That(sources.GetClassifier(), Is.EqualTo("sources"));
        Assert.That(sources.GetUpdated(), Is.EqualTo("20250101010203"));
    }

    [Test]
    public void Parse_ReadsLocalSnapshotMetadataWithoutTimestampOrBuildNumber()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateLocalSnapshotMetadata());

        Assert.That(metadata.GetSnapshotMetadata().IsLocalCopy(), Is.True);
        Assert.That(metadata.GetSnapshotMetadata().HasTimestamp(), Is.False);
        Assert.That(metadata.GetSnapshotMetadata().GetBuildNumber(), Is.EqualTo(0));
    }

    [Test]
    public void ParseVersions_AndParseSnapshotVersions_ReturnDefensiveArrays()
    {
        string[] versions = MavenVersions.ParseMetadataVersions(CreateReleaseMetadata());
        versions[0] = "9.9.9";

        MavenSnapshotVersionMetadata[] snapshotVersions = MavenVersions.ParseSnapshotVersions(CreateSnapshotMetadata());
        snapshotVersions[0] = MavenSnapshotVersionMetadata.Create("pom", null, "1.0-20250101.010203-7", "");

        Assert.That(MavenVersions.ParseMetadataVersions(CreateReleaseMetadata())[0], Is.EqualTo("1.0.0"));
        Assert.That(MavenVersions.ParseSnapshotVersions(CreateSnapshotMetadata())[0].GetExtension(), Is.EqualTo("jar"));
    }

    [Test]
    public void RepositoryMetadata_DefensivelyCopiesVersionsAndSnapshotVersions()
    {
        MavenSnapshotVersionMetadata binary = MavenSnapshotVersionMetadata.Create("jar", null, "1.0-20250101.010203-7", "");
        string[] versions = new string[] { "1.0.0" };
        MavenSnapshotVersionMetadata[] snapshotVersions = new MavenSnapshotVersionMetadata[] { binary };
        MavenRepositoryMetadata metadata = new MavenRepositoryMetadata(
            new MavenGroupId("com.example"),
            new MavenArtifactId("demo"),
            null,
            "",
            "",
            versions,
            "",
            null,
            snapshotVersions);

        versions[0] = "2.0.0";
        snapshotVersions[0] = MavenSnapshotVersionMetadata.Create("pom", null, "1.0-20250101.010203-7", "");
        string[] returnedVersions = metadata.GetVersions();
        MavenSnapshotVersionMetadata[] returnedSnapshotVersions = metadata.GetSnapshotVersions();
        returnedVersions[0] = "3.0.0";
        returnedSnapshotVersions[0] = MavenSnapshotVersionMetadata.Create("war", null, "1.0-20250101.010203-7", "");

        Assert.That(metadata.GetVersion(0), Is.EqualTo("1.0.0"));
        Assert.That(metadata.GetSnapshotVersion(0).GetExtension(), Is.EqualTo("jar"));
    }

    [Test]
    public void Parse_RejectsMalformedMissingAndInvalidMetadata()
    {
        Assert.That(
            delegate
            {
                MavenMetadataParser.Parse("<project />");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                MavenMetadataParser.Parse("<metadata><artifactId>demo</artifactId></metadata>");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                MavenMetadataParser.Parse(CreateInvalidVersionMetadata());
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                MavenMetadataParser.Parse(CreateSnapshotMetadataWithoutRootVersion());
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                MavenMetadataParser.Parse(CreateSnapshotMetadataWithInvalidBuildNumber());
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                MavenMetadataParser.Parse(CreateSnapshotMetadataWithOverflowingBuildNumber());
            },
            Throws.TypeOf<ArgumentException>());
    }

    static string CreateReleaseMetadata()
    {
        return """
            <metadata>
              <groupId>com.example</groupId>
              <artifactId>demo</artifactId>
              <versioning>
                <latest>1.2.0</latest>
                <release>1.1.0</release>
                <versions>
                  <version>1.0.0</version>
                  <version>1.1.0</version>
                  <version>1.2.0</version>
                </versions>
                <lastUpdated>20250101010203</lastUpdated>
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
                <lastUpdated>20250101010203</lastUpdated>
                <snapshotVersions>
                  <snapshotVersion>
                    <extension>jar</extension>
                    <value>1.0-20250101.010203-7</value>
                    <updated>20250101010203</updated>
                  </snapshotVersion>
                  <snapshotVersion>
                    <extension>jar</extension>
                    <classifier>sources</classifier>
                    <value>1.0-20250101.010203-7</value>
                    <updated>20250101010203</updated>
                  </snapshotVersion>
                </snapshotVersions>
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

    static string CreateInvalidVersionMetadata()
    {
        return """
            <metadata>
              <groupId>com.example</groupId>
              <artifactId>demo</artifactId>
              <versioning>
                <versions>
                  <version>bad version</version>
                </versions>
              </versioning>
            </metadata>
            """;
    }

    static string CreateSnapshotMetadataWithoutRootVersion()
    {
        return """
            <metadata>
              <groupId>com.example</groupId>
              <artifactId>demo</artifactId>
              <versioning>
                <snapshot>
                  <timestamp>20250101.010203</timestamp>
                  <buildNumber>7</buildNumber>
                </snapshot>
              </versioning>
            </metadata>
            """;
    }

    static string CreateSnapshotMetadataWithInvalidBuildNumber()
    {
        return """
            <metadata>
              <groupId>com.example</groupId>
              <artifactId>demo</artifactId>
              <version>1.0-SNAPSHOT</version>
              <versioning>
                <snapshot>
                  <timestamp>20250101.010203</timestamp>
                  <buildNumber>seven</buildNumber>
                </snapshot>
              </versioning>
            </metadata>
            """;
    }

    static string CreateSnapshotMetadataWithOverflowingBuildNumber()
    {
        return """
            <metadata>
              <groupId>com.example</groupId>
              <artifactId>demo</artifactId>
              <version>1.0-SNAPSHOT</version>
              <versioning>
                <snapshot>
                  <timestamp>20250101.010203</timestamp>
                  <buildNumber>2147483648</buildNumber>
                </snapshot>
              </versioning>
            </metadata>
            """;
    }
}
