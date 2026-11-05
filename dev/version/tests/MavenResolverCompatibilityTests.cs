using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenResolverCompatibilityTests
{
    [Test]
    public void RepositoryMetadataSelection_ParsesMavenStyleMetadataAndSelectsVersions()
    {
        MavenRepositoryMetadata metadata = MavenVersions.ParseMetadata(CreateReleaseMetadata());

        Assert.That(metadata.GetGroupId().MatchesText("org.example"), Is.True);
        Assert.That(metadata.GetArtifactId().MatchesText("library"), Is.True);
        Assert.That(metadata.GetVersionCount(), Is.EqualTo(4));
        Assert.That(metadata.GetLastUpdated(), Is.EqualTo("20250102030405"));
        AssertIdentity(MavenVersions.ResolveReleaseVersion(metadata), "org.example:library:jar:2.0.0");
        AssertIdentity(MavenVersions.ResolveLatestVersion(metadata), "org.example:library:jar:2.1.0-SNAPSHOT");
        AssertIdentity(MavenVersions.ResolveHighestRangeVersion(metadata, "[1.0,2.0)"), "org.example:library:jar:1.2.3");
    }

    [Test]
    public void SnapshotTimestampSelection_UsesSnapshotVersionEntriesAndTimestampFallback()
    {
        MavenRepositoryMetadata entryMetadata = MavenVersions.ParseMetadata(CreateSnapshotMetadataWithEntries());
        MavenRepositoryMetadata fallbackMetadata = MavenVersions.ParseMetadata(CreateSnapshotMetadataWithoutEntries());

        AssertIdentity(
            MavenVersions.ResolveSnapshotVersion(entryMetadata, "jar", "sources"),
            "org.example:snapshot-demo:jar:sources:1.5-20250102.030405-8");
        AssertIdentity(
            MavenVersions.ResolveSnapshotVersion(fallbackMetadata, "war", null),
            "org.example:snapshot-demo:war:1.5-20250102.030405-8");
    }

    [Test]
    public void DependencyResolution_UsesMavenRangeSelectionAndNearestWinsMediation()
    {
        MavenDependencyModel app = Model(
            "org.example",
            "app",
            "1.0.0",
            new MavenDependency[]
            {
                Dep("org.example", "middle", "[1.0.0,3.0.0)"),
                Dep("org.example", "shared", "[2.0.0]")
            });
        MavenDependencyModel middle1 = Model(
            "org.example",
            "middle",
            "1.0.0",
            new MavenDependency[] { Dep("org.example", "shared", "[1.0.0]") });
        MavenDependencyModel middle2 = Model("org.example", "middle", "2.0.0", new MavenDependency[0]);
        MavenDependencyModel shared1 = Model("org.example", "shared", "1.0.0", new MavenDependency[0]);
        MavenDependencyModel shared2 = Model("org.example", "shared", "2.0.0", new MavenDependency[0]);

        MavenDependencyResolutionResult result = MavenVersions.ResolveDependencies(
            new MavenDependency[] { Dep("org.example", "app", "[1.0.0]") },
            new MavenDependencyModel[] { app, middle2, middle1, shared1, shared2 });

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "org.example", "app", "1.0.0", 0, true, MavenDependencyScope.Compile);
        AssertSelected(result, "org.example", "middle", "1.0.0", 1, false, MavenDependencyScope.Compile);
        AssertSelected(result, "org.example", "shared", "2.0.0", 1, false, MavenDependencyScope.Compile);
    }

    [Test]
    public void DependencyResolution_HonorsExclusionsOptionalDependenciesAndOmittedScopes()
    {
        MavenDependency dependencyOnMiddle = new MavenDependency(
            MavenArtifactCoordinate.CreateJar("org.example", "middle"),
            "[1.0.0]",
            MavenDependencyScope.Compile,
            false,
            new MavenDependencyExclusion[] { MavenDependencyExclusion.Create("org.example", "excluded") });
        MavenDependency optionalDependency = new MavenDependency(
            MavenArtifactCoordinate.CreateJar("org.example", "optional"),
            "[1.0.0]",
            MavenDependencyScope.Compile,
            true,
            new MavenDependencyExclusion[0]);
        MavenDependencyModel app = Model(
            "org.example",
            "app",
            "1.0.0",
            new MavenDependency[] { dependencyOnMiddle });
        MavenDependencyModel middle = Model(
            "org.example",
            "middle",
            "1.0.0",
            new MavenDependency[]
            {
                Dep("org.example", "excluded", "[1.0.0]"),
                optionalDependency,
                ScopedDep("org.example", "provided-only", "[1.0.0]", MavenDependencyScope.Provided),
                ScopedDep("org.example", "test-only", "[1.0.0]", MavenDependencyScope.Test),
                ScopedDep("org.example", "system-only", "[1.0.0]", MavenDependencyScope.System)
            });

        MavenDependencyResolutionResult result = MavenVersions.ResolveDependencies(
            new MavenDependency[] { Dep("org.example", "app", "[1.0.0]") },
            new MavenDependencyModel[]
            {
                app,
                middle,
                Model("org.example", "excluded", "1.0.0", new MavenDependency[0]),
                Model("org.example", "optional", "1.0.0", new MavenDependency[0]),
                Model("org.example", "provided-only", "1.0.0", new MavenDependency[0]),
                Model("org.example", "test-only", "1.0.0", new MavenDependency[0]),
                Model("org.example", "system-only", "1.0.0", new MavenDependency[0])
            });

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "org.example", "app", "1.0.0", 0, true, MavenDependencyScope.Compile);
        AssertSelected(result, "org.example", "middle", "1.0.0", 1, false, MavenDependencyScope.Compile);
        Assert.That(result.HasSelectedArtifact("org.example", "excluded"), Is.False);
        Assert.That(result.HasSelectedArtifact("org.example", "optional"), Is.False);
        Assert.That(result.HasSelectedArtifact("org.example", "provided-only"), Is.False);
        Assert.That(result.HasSelectedArtifact("org.example", "test-only"), Is.False);
        Assert.That(result.HasSelectedArtifact("org.example", "system-only"), Is.False);
    }

    [Test]
    public void DependencyResolution_PropagatesRuntimeScopeToTransitiveCompileDependencies()
    {
        MavenDependencyModel app = Model(
            "org.example",
            "app",
            "1.0.0",
            new MavenDependency[] { ScopedDep("org.example", "runtime-lib", "[1.0.0]", MavenDependencyScope.Runtime) });
        MavenDependencyModel runtime = Model(
            "org.example",
            "runtime-lib",
            "1.0.0",
            new MavenDependency[]
            {
                Dep("org.example", "compile-transitive", "[1.0.0]"),
                ScopedDep("org.example", "runtime-transitive", "[1.0.0]", MavenDependencyScope.Runtime)
            });

        MavenDependencyResolutionResult result = MavenVersions.ResolveDependencies(
            new MavenDependency[] { Dep("org.example", "app", "[1.0.0]") },
            new MavenDependencyModel[]
            {
                app,
                runtime,
                Model("org.example", "compile-transitive", "1.0.0", new MavenDependency[0]),
                Model("org.example", "runtime-transitive", "1.0.0", new MavenDependency[0])
            });

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "org.example", "runtime-lib", "1.0.0", 1, false, MavenDependencyScope.Runtime);
        AssertSelected(result, "org.example", "compile-transitive", "1.0.0", 2, false, MavenDependencyScope.Runtime);
        AssertSelected(result, "org.example", "runtime-transitive", "1.0.0", 2, false, MavenDependencyScope.Runtime);
    }

    static MavenDependency Dep(string groupId, string artifactId, string versionRange)
    {
        return MavenDependency.Create(groupId, artifactId, versionRange);
    }

    static MavenDependency ScopedDep(string groupId, string artifactId, string versionRange, MavenDependencyScope scope)
    {
        return new MavenDependency(
            MavenArtifactCoordinate.CreateJar(groupId, artifactId),
            versionRange,
            scope,
            false,
            new MavenDependencyExclusion[0]);
    }

    static MavenDependencyModel Model(string groupId, string artifactId, string version, MavenDependency[] dependencies)
    {
        return new MavenDependencyModel(
            MavenArtifactVersionIdentity.Create(groupId, artifactId, "jar", null, version),
            null,
            dependencies,
            MavenDependencyManagement.CreateEmpty());
    }

    static void AssertSelected(
        MavenDependencyResolutionResult result,
        string groupId,
        string artifactId,
        string version,
        int depth,
        bool direct,
        MavenDependencyScope scope)
    {
        MavenDependencyGraphNode node = result.GetSelectedArtifact(groupId, artifactId);

        Assert.That(node.GetIdentity().GetVersionText(), Is.EqualTo(version));
        Assert.That(node.GetDepth(), Is.EqualTo(depth));
        Assert.That(node.IsDirect(), Is.EqualTo(direct));
        Assert.That(node.GetEffectiveScope(), Is.EqualTo(scope));
    }

    static void AssertIdentity(MavenArtifactVersionIdentity? identity, string identityText)
    {
        System.ArgumentNullException.ThrowIfNull(identity);
        Assert.That(identity, Is.Not.Null);
        Assert.That(identity!.GetIdentityText(), Is.EqualTo(identityText));
    }

    static string CreateReleaseMetadata()
    {
        return """
            <metadata xmlns="https://maven.apache.org/METADATA/1.1.0">
              <groupId>org.example</groupId>
              <artifactId>library</artifactId>
              <versioning>
                <latest>2.1.0-SNAPSHOT</latest>
                <release>2.0.0</release>
                <versions>
                  <version>1.0.0</version>
                  <version>1.2.3</version>
                  <version>2.0.0</version>
                  <version>2.1.0-SNAPSHOT</version>
                </versions>
                <lastUpdated>20250102030405</lastUpdated>
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
                  <snapshotVersion>
                    <extension>jar</extension>
                    <classifier>sources</classifier>
                    <value>1.5-20250102.030405-8</value>
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
              <groupId>org.example</groupId>
              <artifactId>snapshot-demo</artifactId>
              <version>1.5-SNAPSHOT</version>
              <versioning>
                <snapshot>
                  <timestamp>20250102.030405</timestamp>
                  <buildNumber>8</buildNumber>
                </snapshot>
              </versioning>
            </metadata>
            """;
    }
}
