using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenResolverDiagnosticTests
{
    [Test]
    public void ParseMetadataWithDiagnostics_ReportsMalformedMetadata()
    {
        MavenMetadataParseResult result = MavenVersions.ParseMetadataWithDiagnostics("<project />");

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasMetadata(), Is.False);
        AssertDiagnostic(result, MavenMetadataDiagnosticCodes.MalformedMetadata);
    }

    [Test]
    public void ParseMetadataWithDiagnostics_ReportsMissingSnapshotVersion()
    {
        MavenMetadataParseResult result = MavenVersions.ParseMetadataWithDiagnostics(CreateSnapshotMetadataWithoutRootVersion());

        Assert.That(result.IsFailure(), Is.True);
        AssertDiagnostic(result, MavenMetadataDiagnosticCodes.MissingSnapshotMetadata);
    }

    [Test]
    public void ResolveSnapshotVersionWithDiagnostics_ReportsMissingSnapshotVersionEntry()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateSnapshotMetadataWithEntries());

        MavenArtifactVersionResolutionResult result = MavenVersions.ResolveSnapshotVersionWithDiagnostics(metadata, "pom", null);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasIdentity(), Is.False);
        AssertDiagnostic(result, MavenMetadataDiagnosticCodes.MissingSnapshotMetadata);
    }

    [Test]
    public void Resolve_ReportsUnresolvedArtifactCoordinateDiagnostic()
    {
        MavenDependencyResolutionResult result = MavenVersions.ResolveDependencies(
            new MavenDependency[] { MavenDependency.Create("org.example", "missing", "[1.0.0]") },
            new MavenDependencyModel[0]);

        AssertConflict(result, "missing", MavenResolverDiagnosticCodes.UnresolvedArtifactCoordinate);
    }

    [Test]
    public void Resolve_ReportsConflictingRangesDiagnostic()
    {
        MavenDependencyResolutionResult result = MavenVersions.ResolveDependencies(
            new MavenDependency[] { MavenDependency.Create("org.example", "library", "[2.0.0]") },
            new MavenDependencyModel[] { Model("org.example", "library", "1.0.0") });

        AssertConflict(result, "library", MavenResolverDiagnosticCodes.ConflictingRanges);
    }

    [Test]
    public void Resolve_ReportsUnsupportedResolverFeatureDiagnostic()
    {
        MavenDependency rootSystemDependency = new MavenDependency(
            MavenArtifactCoordinate.CreateJar("org.example", "system-lib"),
            "[1.0.0]",
            MavenDependencyScope.System,
            false,
            new MavenDependencyExclusion[0]);

        MavenDependencyResolutionResult result = MavenVersions.ResolveDependencies(
            new MavenDependency[] { rootSystemDependency },
            new MavenDependencyModel[] { Model("org.example", "system-lib", "1.0.0") });

        AssertConflict(result, "system-lib", MavenResolverDiagnosticCodes.UnsupportedFeature);
    }

    [Test]
    public void DependencyConflict_DefaultConstructorUsesConflictingRangesDiagnostic()
    {
        MavenDependencyConflict conflict = MavenDependencyConflict.Create(
            MavenArtifactCoordinate.CreateJar("org.example", "library"),
            0,
            "No model satisfies the requested range.");

        Assert.That(conflict.GetDiagnosticCode(), Is.EqualTo(MavenResolverDiagnosticCodes.ConflictingRanges));
        Assert.That(conflict.GetMessage(), Is.EqualTo("No model satisfies the requested range."));
    }

    static void AssertConflict(MavenDependencyResolutionResult result, string artifactId, string diagnosticCode)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetConflictCount(), Is.EqualTo(1));
        Assert.That(result.GetConflict(0).GetCoordinate().GetArtifactId().MatchesText(artifactId), Is.True);
        Assert.That(result.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(diagnosticCode));
        Assert.That(result.GetConflict(0).GetMessage(), Is.Not.Empty);
    }

    static void AssertDiagnostic(MavenMetadataParseResult result, string diagnosticCode)
    {
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(diagnosticCode));
        Assert.That(result.GetDiagnostic(0).GetMessage(), Is.Not.Empty);
    }

    static void AssertDiagnostic(MavenArtifactVersionResolutionResult result, string diagnosticCode)
    {
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(diagnosticCode));
        Assert.That(result.GetDiagnostic(0).GetMessage(), Is.Not.Empty);
    }

    static MavenDependencyModel Model(string groupId, string artifactId, string version)
    {
        return new MavenDependencyModel(
            MavenArtifactVersionIdentity.Create(groupId, artifactId, "jar", null, version),
            null,
            new MavenDependency[0],
            MavenDependencyManagement.CreateEmpty());
    }

    static string CreateSnapshotMetadataWithoutRootVersion()
    {
        return """
            <metadata>
              <groupId>org.example</groupId>
              <artifactId>snapshot-demo</artifactId>
              <versioning>
                <snapshot>
                  <timestamp>20250102.030405</timestamp>
                  <buildNumber>8</buildNumber>
                </snapshot>
              </versioning>
            </metadata>
            """;
    }

    static string CreateSnapshotMetadataWithEntries()
    {
        return """
            <metadata>
              <groupId>org.example</groupId>
              <artifactId>snapshot-demo</artifactId>
              <version>1.5-SNAPSHOT</version>
              <versioning>
                <snapshot>
                  <timestamp>20250102.030405</timestamp>
                  <buildNumber>8</buildNumber>
                </snapshot>
                <snapshotVersions>
                  <snapshotVersion>
                    <extension>jar</extension>
                    <value>1.5-20250102.030405-8</value>
                  </snapshotVersion>
                </snapshotVersions>
              </versioning>
            </metadata>
            """;
    }
}
