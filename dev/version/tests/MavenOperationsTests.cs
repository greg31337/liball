using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenOperationsTests
{
    [Test]
    public void ComparableVersionOperations_ParseNormalizeAndCompare()
    {
        IMavenResolution operations = new MavenOperations();

        VersionParseResult parsed = operations.ParseVersion("01.0.0-final");
        VersionParseResult invalid = operations.ParseVersion("bad version");

        Assert.That(parsed.success, Is.True);
        Assert.That(parsed.dialect, Is.EqualTo(VersionDialect.Maven));
        Assert.That(parsed.version, Is.Not.Null);
        Assert.That(operations.NormalizeVersion(parsed.sourceText), Is.EqualTo("1"));
        Assert.That(operations.NormalizeVersion("1_0_RC1"), Is.EqualTo("1-0-rc1"));
        Assert.That(operations.CompareVersions("1.0-alpha", "1.0-beta"), Is.LessThan(0));
        Assert.That(operations.CompareVersions("1.0", "1.0-ga"), Is.Zero);
        Assert.That(operations.CompareVersions("1.0-sp", "1.0"), Is.GreaterThan(0));
        Assert.That(invalid.success, Is.False);
        Assert.That(invalid.diagnostics, Is.Not.Empty);
    }

    [Test]
    public void RangeOperations_ParseIntervalUnionsAndExclusions()
    {
        IMavenResolution operations = new MavenOperations();

        VersionRangeParseResult union = operations.ParseRange("(,1.0],[1.2,)");
        VersionRangeParseResult exclusion = operations.ParseRange("(,1.1),(1.1,)");
        VersionRangeParseResult malformed = operations.ParseRange("[2.0,1.0]");

        Assert.That(union.success, Is.True);
        Assert.That(union.syntax, Is.EqualTo(RangeSyntax.Maven));
        Assert.That(union.range, Is.Not.Null);
        Assert.That(union.range!.sets, Has.Length.EqualTo(2));
        Assert.That(exclusion.success, Is.True);
        Assert.That(exclusion.range, Is.Not.Null);
        Assert.That(exclusion.range!.sets, Has.Length.EqualTo(2));
        Assert.That(exclusion.range.sets[0].comparators[0].rangeOperator, Is.EqualTo(RangeOperator.LessThan));
        Assert.That(exclusion.range.sets[1].comparators[0].rangeOperator, Is.EqualTo(RangeOperator.GreaterThan));
        Assert.That(malformed.success, Is.False);
        Assert.That(malformed.diagnostics, Is.Not.Empty);
    }

    [Test]
    public void RepositoryMetadataOperations_ParseAndSelectVersions()
    {
        IMavenResolution operations = new MavenOperations();
        string xml = ReleaseMetadataXml();

        MavenMetadataParseResult parsed = operations.ParseMetadata(xml);
        string[] versions = operations.ParseMetadataVersions(xml);

        Assert.That(parsed.success, Is.True);
        Assert.That(parsed.metadata, Is.Not.Null);
        Assert.That(parsed.metadata!.groupId.value, Is.EqualTo("com.example"));
        Assert.That(parsed.metadata.artifactId.value, Is.EqualTo("demo"));
        Assert.That(versions, Is.EqualTo(new string[]
        {
            "1.0.0",
            "1.5.0",
            "2.0.0",
            "3.0.0-SNAPSHOT"
        }));

        MavenArtifactVersionIdentity? release = operations.ResolveReleaseVersion(parsed.metadata);
        MavenArtifactVersionIdentity? latest = operations.ResolveLatestVersion(parsed.metadata);
        MavenArtifactVersionIdentity? ranged = operations.ResolveHighestRangeVersion(
            parsed.metadata,
            "[1.0.0,2.0.0)");

        AssertVersion(release, "1.5.0");
        AssertVersion(latest, "3.0.0-SNAPSHOT");
        AssertVersion(ranged, "1.5.0");
    }

    [Test]
    public void SnapshotMetadataOperations_ResolveEntriesThroughEveryPublicForm()
    {
        IMavenResolution operations = new MavenOperations();
        string xml = SnapshotMetadataXml();
        MavenMetadataParseResult parsed = operations.ParseMetadata(xml);

        Assert.That(parsed.success, Is.True);
        Assert.That(parsed.metadata, Is.Not.Null);
        MavenSnapshotVersionMetadata[] entries = operations.ParseSnapshotVersions(xml);
        Assert.That(entries, Has.Length.EqualTo(2));
        Assert.That(entries[1].classifier, Is.EqualTo("sources"));

        MavenArtifactVersionIdentity baseIdentity = Identity(
            operations,
            "com.example",
            "demo",
            "jar",
            "sources",
            "1.0-SNAPSHOT");
        MavenArtifactVersionIdentity? fromMetadata = operations.ResolveSnapshotVersion(
            parsed.metadata!,
            "jar",
            "sources");
        MavenArtifactVersionResolutionResult withDiagnostics =
            operations.ResolveSnapshotVersionWithDiagnostics(parsed.metadata, "jar", "sources");
        MavenArtifactVersionIdentity? fromIdentity = operations.ResolveSnapshotVersionForIdentity(
            baseIdentity,
            parsed.metadata,
            "jar",
            "sources");
        MavenArtifactVersionIdentity? fromCoordinate = operations.ResolveSnapshotVersionForCoordinate(
            "com.example",
            "demo",
            "1.0-SNAPSHOT",
            parsed.metadata,
            "jar",
            "sources");

        AssertVersion(fromMetadata, "1.0-20250101.010203-7");
        Assert.That(withDiagnostics.success, Is.True);
        AssertVersion(withDiagnostics.identity, "1.0-20250101.010203-7");
        AssertVersion(fromIdentity, "1.0-20250101.010203-7");
        AssertVersion(fromCoordinate, "1.0-20250101.010203-7");
    }

    [Test]
    public void ArtifactResolution_PreservesReleasesAndResolvesRemoteSnapshots()
    {
        IMavenResolution operations = new MavenOperations();
        MavenArtifactVersionIdentity releaseIdentity = Identity(
            operations,
            "com.example",
            "demo",
            "jar",
            null,
            "1.0.0");
        MavenArtifactMetadata releaseMetadata = new MavenArtifactMetadata(
            releaseIdentity,
            new MavenRepositoryId("central"),
            null);
        MavenArtifactVersionIdentity snapshotIdentity = Identity(
            operations,
            "com.example",
            "demo",
            "jar",
            "sources",
            "2.0-SNAPSHOT");
        MavenSnapshotMetadata snapshot = new MavenSnapshotMetadata(
            snapshotIdentity,
            "20250101.010203",
            4,
            "20250101010203",
            false);
        MavenArtifactMetadata snapshotArtifact = new MavenArtifactMetadata(
            snapshotIdentity,
            new MavenRepositoryId("central"),
            snapshot);

        MavenArtifactVersionResolutionResult release = operations.ResolveArtifactVersion(releaseMetadata);
        MavenArtifactVersionResolutionResult resolvedSnapshot = operations.ResolveArtifactVersion(
            snapshotArtifact);

        Assert.That(release.success, Is.True);
        Assert.That(release.identity, Is.SameAs(releaseIdentity));
        Assert.That(resolvedSnapshot.success, Is.True);
        Assert.That(resolvedSnapshot.identity, Is.Not.Null);
        Assert.That(resolvedSnapshot.identity!.coordinate.classifier, Is.EqualTo("sources"));
        AssertVersion(resolvedSnapshot.identity, "2.0-20250101.010203-4");
    }

    [Test]
    public void DependencyResolution_CoversDefaultAndRootManagementContracts()
    {
        IMavenResolution operations = new MavenOperations();
        MavenDependency root = new MavenDependency(
            Coordinate("com.example", "app", "jar", null),
            null,
            MavenDependencyScope.Compile,
            false,
            new MavenDependencyExclusion[0]);
        MavenDependencyModel appOne = Model(
            operations,
            "com.example",
            "app",
            "1.0.0",
            new MavenDependency[0]);
        MavenDependencyModel appTwo = Model(
            operations,
            "com.example",
            "app",
            "2.0.0",
            new MavenDependency[0]);
        MavenDependencyModel[] models = new MavenDependencyModel[] { appOne, appTwo };

        MavenDependencyResolutionResult unresolved = operations.ResolveDependencies(
            new MavenDependency[] { root },
            models);
        MavenDependencyManagement management = new MavenDependencyManagement(
            new MavenDependency[] { Dependency("com.example", "app", "[2.0.0]") },
            new MavenImportedBomConstraint[0]);
        MavenDependencyResolutionResult resolved = operations.ResolveDependenciesWithManagement(
            new MavenDependency[] { root },
            management,
            models);

        Assert.That(unresolved.conflicts, Is.Empty);
        Assert.That(unresolved.nodes, Has.Length.EqualTo(1));
        Assert.That(unresolved.nodes[0].model, Is.SameAs(appOne));
        Assert.That(resolved.conflicts, Is.Empty);
        Assert.That(resolved.nodes, Has.Length.EqualTo(1));
        Assert.That(resolved.nodes[0].model, Is.SameAs(appTwo));
    }

    [Test]
    public void DiagnosticOperations_ReturnFailuresForMalformedMetadataAndMissingSnapshots()
    {
        IMavenResolution operations = new MavenOperations();

        MavenMetadataParseResult malformed = operations.ParseMetadata("<project />");
        MavenMetadataParseResult release = operations.ParseMetadata(ReleaseMetadataXml());

        Assert.That(malformed.success, Is.False);
        Assert.That(malformed.metadata, Is.Null);
        Assert.That(malformed.diagnostics, Has.Length.EqualTo(1));
        Assert.That(
            malformed.diagnostics[0].code,
            Is.EqualTo(MavenMetadataDiagnosticCodes.MalformedMetadata));
        Assert.That(release.success, Is.True);
        MavenArtifactVersionResolutionResult missing =
            operations.ResolveSnapshotVersionWithDiagnostics(release.metadata!, "jar", null);
        Assert.That(missing.success, Is.False);
        Assert.That(missing.identity, Is.Null);
        Assert.That(
            missing.diagnostics[0].code,
            Is.EqualTo(MavenMetadataDiagnosticCodes.MissingSnapshotMetadata));
    }

    static MavenArtifactCoordinate Coordinate(
        string groupId,
        string artifactId,
        string packaging,
        string? classifier)
    {
        return new MavenArtifactCoordinate(
            new MavenGroupId(groupId),
            new MavenArtifactId(artifactId),
            packaging,
            classifier);
    }

    static MavenArtifactVersionIdentity Identity(
        IMavenResolution operations,
        string groupId,
        string artifactId,
        string packaging,
        string? classifier,
        string versionText)
    {
        VersionParseResult version = operations.ParseVersion(versionText);
        Assert.That(version.success, Is.True, versionText);
        Assert.That(version.version, Is.Not.Null, versionText);
        return new MavenArtifactVersionIdentity(
            Coordinate(groupId, artifactId, packaging, classifier),
            version.version!);
    }

    static MavenDependency Dependency(string groupId, string artifactId, string versionText)
    {
        return new MavenDependency(
            Coordinate(groupId, artifactId, "jar", null),
            versionText,
            MavenDependencyScope.Compile,
            false,
            new MavenDependencyExclusion[0]);
    }

    static MavenDependencyModel Model(
        IMavenResolution operations,
        string groupId,
        string artifactId,
        string versionText,
        MavenDependency[] dependencies)
    {
        return new MavenDependencyModel(
            Identity(operations, groupId, artifactId, "jar", null, versionText),
            null,
            dependencies,
            new MavenDependencyManagement(
                new MavenDependency[0],
                new MavenImportedBomConstraint[0]));
    }

    static void AssertVersion(MavenArtifactVersionIdentity? identity, string versionText)
    {
        Assert.That(identity, Is.Not.Null);
        Assert.That(identity!.version.originalText, Is.EqualTo(versionText));
    }

    static string ReleaseMetadataXml()
    {
        return @"<metadata>
  <groupId>com.example</groupId>
  <artifactId>demo</artifactId>
  <versioning>
    <latest>3.0.0-SNAPSHOT</latest>
    <release>1.5.0</release>
    <versions>
      <version>1.0.0</version>
      <version>1.5.0</version>
      <version>2.0.0</version>
      <version>3.0.0-SNAPSHOT</version>
    </versions>
  </versioning>
</metadata>";
    }

    static string SnapshotMetadataXml()
    {
        return @"<metadata>
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
</metadata>";
    }
}
