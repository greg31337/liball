using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PublicMavenOperationsCompletenessTests
{
    [TestCase("1.0-alpha_2", "1.0-alpha-2")]
    [TestCase("1..0--alpha__2", "1.0-alpha-2")]
    [TestCase("01.000.0007", "1.0.7")]
    [TestCase("1.0.0-final", "1")]
    public void ComparableVersion_HandlesSeparatorsAndNormalization(string text, string normalized)
    {
        IMavenResolution operations = new MavenOperations();

        VersionParseResult parsed = operations.ParseVersion(text);

        Assert.That(parsed.success, Is.True, text);
        Assert.That(parsed.dialect, Is.EqualTo(VersionDialect.Maven), text);
        Assert.That(parsed.version, Is.Not.Null, text);
        Assert.That(operations.NormalizeVersion(text), Is.EqualTo(normalized), text);
    }

    [Test]
    public void ComparableVersion_SplitsDigitLetterBoundariesAndSupportsUnlimitedIntegers()
    {
        IMavenResolution operations = new MavenOperations();
        string smaller = "1.9999999999999999999999999999999999999999";
        string larger = "1.10000000000000000000000000000000000000000";

        Assert.That(operations.CompareVersions("1.0alpha1", "1.0alpha2"), Is.LessThan(0));
        Assert.That(operations.CompareVersions("2024rc9", "2024rc10"), Is.LessThan(0));
        Assert.That(operations.CompareVersions("1.0RC1", "1.0-rc2"), Is.LessThan(0));
        Assert.That(operations.CompareVersions(smaller, larger), Is.LessThan(0));
        Assert.That(
            operations.CompareVersions(
                "1.00000000000000000000000000000000000000010",
                "1.10"),
            Is.Zero);
    }

    [TestCase("a", "alpha")]
    [TestCase("b", "beta")]
    [TestCase("m", "milestone")]
    [TestCase("cr", "rc")]
    [TestCase("ga", "")]
    [TestCase("final", "")]
    [TestCase("release", "")]
    public void ComparableVersion_TreatsQualifierAliasesAsEquivalent(string alias, string canonical)
    {
        IMavenResolution operations = new MavenOperations();
        string left = "1-" + alias;
        string right = canonical.Length == 0 ? "1" : "1-" + canonical;

        Assert.That(operations.CompareVersions(left, right), Is.Zero);
        Assert.That(operations.CompareVersions(right, left), Is.Zero);
    }

    [Test]
    public void ComparableVersion_OrdersKnownAndUnknownQualifiersAndTrimsNullItems()
    {
        IMavenResolution operations = new MavenOperations();
        string[] ascending = new string[]
        {
            "1-alpha",
            "1-beta",
            "1-milestone",
            "1-rc",
            "1-snapshot",
            "1",
            "1-sp",
            "1-unknown"
        };
        int index = 0;

        while (index < ascending.Length - 1)
        {
            Assert.That(
                operations.CompareVersions(ascending[index], ascending[index + 1]),
                Is.LessThan(0),
                ascending[index] + " < " + ascending[index + 1]);
            index = index + 1;
        }

        Assert.That(operations.CompareVersions("1", "1.0.0.0"), Is.Zero);
        Assert.That(operations.CompareVersions("1", "1-ga-final-release"), Is.Zero);
        Assert.That(operations.CompareVersions("1-alpha", "1-alpha.0"), Is.Zero);
        Assert.That(operations.CompareVersions("1-zeta", "1-omega"), Is.GreaterThan(0));
    }

    [Test]
    public void RangeOperations_DistinguishSoftRequirementsFromHardIntervals()
    {
        IMavenResolution operations = new MavenOperations();

        VersionRangeParseResult soft = operations.ParseRange("1.0");
        VersionRangeParseResult exact = operations.ParseRange("[1.0]");
        VersionRangeParseResult bounded = operations.ParseRange("[1.0,2.0)");

        Assert.That(soft.success, Is.True);
        Assert.That(soft.range, Is.Not.Null);
        Assert.That(soft.range!.sets, Has.Length.EqualTo(1));
        Assert.That(soft.range.sets[0].comparators, Is.Empty);
        Assert.That(exact.success, Is.True);
        Assert.That(exact.range, Is.Not.Null);
        Assert.That(exact.range!.sets[0].comparators, Has.Length.EqualTo(1));
        Assert.That(exact.range.sets[0].comparators[0].rangeOperator, Is.EqualTo(RangeOperator.Equal));
        Assert.That(bounded.success, Is.True);
        Assert.That(bounded.range, Is.Not.Null);
        Assert.That(bounded.range!.sets[0].comparators, Has.Length.EqualTo(2));
        Assert.That(
            bounded.range.sets[0].comparators[0].rangeOperator,
            Is.EqualTo(RangeOperator.GreaterThanOrEqual));
        Assert.That(
            bounded.range.sets[0].comparators[1].rangeOperator,
            Is.EqualTo(RangeOperator.LessThan));
    }

    [Test]
    public void RangeOperations_ParseIntervalUnionsAndVersionHoles()
    {
        IMavenResolution operations = new MavenOperations();
        VersionRangeParseResult union = operations.ParseRange("(,1.0],[1.2,)");
        VersionRangeParseResult hole = operations.ParseRange("(,1.1),(1.1,)");
        MavenRepositoryMetadata onlyExcluded = RepositoryMetadata(
            "org.example",
            "library",
            new string[] { "1.1" });
        MavenRepositoryMetadata candidates = RepositoryMetadata(
            "org.example",
            "library",
            new string[] { "0.9", "1.0", "1.1", "1.2", "2.0" });

        Assert.That(union.success, Is.True);
        Assert.That(union.range, Is.Not.Null);
        Assert.That(union.range!.sets, Has.Length.EqualTo(2));
        Assert.That(hole.success, Is.True);
        Assert.That(hole.range, Is.Not.Null);
        Assert.That(hole.range!.sets, Has.Length.EqualTo(2));
        Assert.That(
            operations.ResolveHighestRangeVersion(onlyExcluded, "(,1.1),(1.1,)"),
            Is.Null);
        AssertVersion(
            operations.ResolveHighestRangeVersion(candidates, "(,1.0],[1.2,)"),
            "2.0");
        AssertVersion(
            operations.ResolveHighestRangeVersion(candidates, "[1.0,2.0)"),
            "1.2");
    }

    [Test]
    public void MetadataOperations_ParseNamespacedReleaseMetadataAndDeclaredCandidates()
    {
        IMavenResolution operations = new MavenOperations();
        string xml = ReleaseMetadataXml();

        MavenMetadataParseResult result = operations.ParseMetadata(xml);
        string[] versions = operations.ParseMetadataVersions(xml);

        Assert.That(result.success, Is.True);
        Assert.That(result.metadata, Is.Not.Null);
        Assert.That(result.metadata!.groupId.value, Is.EqualTo("org.example"));
        Assert.That(result.metadata.artifactId.value, Is.EqualTo("library"));
        Assert.That(result.metadata.latestVersion, Is.EqualTo("2.1.0-SNAPSHOT"));
        Assert.That(result.metadata.releaseVersion, Is.EqualTo("2.0.0"));
        Assert.That(result.metadata.lastUpdated, Is.EqualTo("20250102030405"));
        Assert.That(versions, Is.EqualTo(new string[]
        {
            "1.0.0",
            "1.2.3",
            "2.0.0",
            "2.1.0-SNAPSHOT"
        }));
        AssertVersion(operations.ResolveReleaseVersion(result.metadata), "2.0.0");
        AssertVersion(operations.ResolveLatestVersion(result.metadata), "2.1.0-SNAPSHOT");
        AssertVersion(
            operations.ResolveHighestRangeVersion(result.metadata, "[1.0,2.0)"),
            "1.2.3");
    }

    [Test]
    public void MetadataSelection_FallsBackToComparableVersionOrderingWhenMarkersAreAbsent()
    {
        IMavenResolution operations = new MavenOperations();
        MavenRepositoryMetadata metadata = RepositoryMetadata(
            "org.example",
            "library",
            new string[]
            {
                "1.0-rc1",
                "1.0",
                "1.0-sp",
                "2.0-SNAPSHOT"
            });

        AssertVersion(operations.ResolveReleaseVersion(metadata), "1.0-sp");
        AssertVersion(operations.ResolveLatestVersion(metadata), "2.0-SNAPSHOT");
        AssertVersion(
            operations.ResolveHighestRangeVersion(metadata, "[1.0-rc1,1.1)"),
            "1.0-sp");
    }

    [Test]
    public void SnapshotOperations_ParseEntriesAndResolveClassifierMatches()
    {
        IMavenResolution operations = new MavenOperations();
        MavenMetadataParseResult parsed = operations.ParseMetadata(SnapshotEntriesXml());

        Assert.That(parsed.success, Is.True);
        Assert.That(parsed.metadata, Is.Not.Null);
        MavenSnapshotVersionMetadata[] entries = operations.ParseSnapshotVersions(
            SnapshotEntriesXml());
        Assert.That(entries, Has.Length.EqualTo(3));
        Assert.That(entries[0].extension, Is.EqualTo("jar"));
        Assert.That(entries[0].classifierSpecified, Is.False);
        Assert.That(entries[1].classifier, Is.EqualTo("sources"));
        Assert.That(entries[2].extension, Is.EqualTo("pom"));

        AssertVersion(
            operations.ResolveSnapshotVersion(parsed.metadata!, "jar", null),
            "1.5-20250102.030405-8");
        AssertVersion(
            operations.ResolveSnapshotVersion(parsed.metadata, "jar", "sources"),
            "1.5-20250102.030405-8");
        AssertVersion(
            operations.ResolveSnapshotVersionForCoordinate(
                "org.example",
                "snapshot-demo",
                "1.5-SNAPSHOT",
                parsed.metadata,
                "pom",
                null),
            "1.5-20250102.030405-8");
        Assert.That(operations.ResolveSnapshotVersion(parsed.metadata, "war", null), Is.Null);
    }

    [Test]
    public void SnapshotOperations_FallBackToTimestampAndPreserveLocalCopies()
    {
        IMavenResolution operations = new MavenOperations();
        MavenMetadataParseResult remote = operations.ParseMetadata(SnapshotFallbackXml());
        MavenMetadataParseResult local = operations.ParseMetadata(LocalSnapshotXml());

        Assert.That(remote.success, Is.True);
        Assert.That(remote.metadata, Is.Not.Null);
        Assert.That(local.success, Is.True);
        Assert.That(local.metadata, Is.Not.Null);
        AssertVersion(
            operations.ResolveSnapshotVersion(remote.metadata!, "war", "classes"),
            "1.5-20250102.030405-8");
        AssertVersion(
            operations.ResolveSnapshotVersion(local.metadata!, "jar", null),
            "1.5-SNAPSHOT");

        MavenArtifactVersionIdentity remoteIdentity = Identity(
            operations,
            "org.example",
            "snapshot-demo",
            "war",
            "classes",
            "1.5-SNAPSHOT");
        MavenArtifactVersionIdentity localIdentity = Identity(
            operations,
            "org.example",
            "snapshot-demo",
            "jar",
            null,
            "1.5-SNAPSHOT");
        MavenArtifactVersionResolutionResult remoteArtifact = operations.ResolveArtifactVersion(
            new MavenArtifactMetadata(
                remoteIdentity,
                new MavenRepositoryId("central"),
                new MavenSnapshotMetadata(
                    remoteIdentity,
                    "20250102.030405",
                    8,
                    "20250102030405",
                    false)));
        MavenArtifactVersionResolutionResult localArtifact = operations.ResolveArtifactVersion(
            new MavenArtifactMetadata(
                localIdentity,
                new MavenRepositoryId("local"),
                new MavenSnapshotMetadata(localIdentity, "", 0, "", true)));

        Assert.That(remoteArtifact.success, Is.True);
        AssertVersion(remoteArtifact.identity, "1.5-20250102.030405-8");
        Assert.That(localArtifact.success, Is.True);
        Assert.That(localArtifact.identity, Is.SameAs(localIdentity));
    }

    [Test]
    public void DependencyResolution_AppliesRangeSelectionAndNearestWinsMediation()
    {
        IMavenResolution operations = new MavenOperations();
        MavenDependencyModel app = Model(
            operations,
            "org.example",
            "app",
            "1.0.0",
            new MavenDependency[]
            {
                Dependency("org.example", "middle", "[1.0.0,3.0.0)"),
                Dependency("org.example", "shared", "[2.0.0]")
            });
        MavenDependencyModel middleOne = Model(
            operations,
            "org.example",
            "middle",
            "1.0.0",
            new MavenDependency[] { Dependency("org.example", "shared", "[1.0.0]") });
        MavenDependencyModel middleTwo = Model(
            operations,
            "org.example",
            "middle",
            "2.0.0",
            new MavenDependency[0]);
        MavenDependencyModel sharedOne = Model(
            operations,
            "org.example",
            "shared",
            "1.0.0",
            new MavenDependency[0]);
        MavenDependencyModel sharedTwo = Model(
            operations,
            "org.example",
            "shared",
            "2.0.0",
            new MavenDependency[0]);

        MavenDependencyResolutionResult result = operations.ResolveDependencies(
            new MavenDependency[] { Dependency("org.example", "app", "[1.0.0]") },
            new MavenDependencyModel[]
            {
                app,
                middleTwo,
                middleOne,
                sharedOne,
                sharedTwo
            });

        Assert.That(result.conflicts, Is.Empty);
        AssertNode(result, "app", "1.0.0", 0, true, MavenDependencyScope.Compile);
        AssertNode(result, "middle", "1.0.0", 1, false, MavenDependencyScope.Compile);
        AssertNode(result, "shared", "2.0.0", 1, false, MavenDependencyScope.Compile);
    }

    [Test]
    public void DependencyResolution_HonorsExclusionsOptionalDependenciesAndOmittedScopes()
    {
        IMavenResolution operations = new MavenOperations();
        MavenDependency middleDependency = new MavenDependency(
            Coordinate("org.example", "middle", "jar", null),
            "[1.0.0]",
            MavenDependencyScope.Compile,
            false,
            new MavenDependencyExclusion[]
            {
                new MavenDependencyExclusion(
                    new MavenGroupId("org.example"),
                    new MavenArtifactId("excluded"))
            });
        MavenDependency optional = new MavenDependency(
            Coordinate("org.example", "optional", "jar", null),
            "[1.0.0]",
            MavenDependencyScope.Compile,
            true,
            new MavenDependencyExclusion[0]);
        MavenDependencyModel app = Model(
            operations,
            "org.example",
            "app",
            "1.0.0",
            new MavenDependency[] { middleDependency });
        MavenDependencyModel middle = Model(
            operations,
            "org.example",
            "middle",
            "1.0.0",
            new MavenDependency[]
            {
                Dependency("org.example", "excluded", "[1.0.0]"),
                optional,
                ScopedDependency("provided", MavenDependencyScope.Provided),
                ScopedDependency("test", MavenDependencyScope.Test),
                ScopedDependency("system", MavenDependencyScope.System)
            });
        MavenDependencyModel[] models = new MavenDependencyModel[]
        {
            app,
            middle,
            Model(operations, "org.example", "excluded", "1.0.0", new MavenDependency[0]),
            Model(operations, "org.example", "optional", "1.0.0", new MavenDependency[0]),
            Model(operations, "org.example", "provided", "1.0.0", new MavenDependency[0]),
            Model(operations, "org.example", "test", "1.0.0", new MavenDependency[0]),
            Model(operations, "org.example", "system", "1.0.0", new MavenDependency[0])
        };

        MavenDependencyResolutionResult result = operations.ResolveDependencies(
            new MavenDependency[] { Dependency("org.example", "app", "[1.0.0]") },
            models);

        Assert.That(result.conflicts, Is.Empty);
        Assert.That(FindNode(result, "middle"), Is.Not.Null);
        Assert.That(FindNode(result, "excluded"), Is.Null);
        Assert.That(FindNode(result, "optional"), Is.Null);
        Assert.That(FindNode(result, "provided"), Is.Null);
        Assert.That(FindNode(result, "test"), Is.Null);
        Assert.That(FindNode(result, "system"), Is.Null);
    }

    [Test]
    public void DependencyResolution_PropagatesRuntimeScopeAndAppliesRootManagement()
    {
        IMavenResolution operations = new MavenOperations();
        MavenDependencyModel appOne = Model(
            operations,
            "org.example",
            "app",
            "1.0.0",
            new MavenDependency[]
            {
                new MavenDependency(
                    Coordinate("org.example", "runtime", "jar", null),
                    "[1.0.0]",
                    MavenDependencyScope.Runtime,
                    false,
                    new MavenDependencyExclusion[0])
            });
        MavenDependencyModel appTwo = Model(
            operations,
            "org.example",
            "app",
            "2.0.0",
            new MavenDependency[0]);
        MavenDependencyModel runtime = Model(
            operations,
            "org.example",
            "runtime",
            "1.0.0",
            new MavenDependency[] { Dependency("org.example", "transitive", "[1.0.0]") });
        MavenDependencyModel transitive = Model(
            operations,
            "org.example",
            "transitive",
            "1.0.0",
            new MavenDependency[0]);
        MavenDependency unmanagedRoot = new MavenDependency(
            Coordinate("org.example", "app", "jar", null),
            null,
            MavenDependencyScope.Compile,
            false,
            new MavenDependencyExclusion[0]);
        MavenDependencyManagement management = new MavenDependencyManagement(
            new MavenDependency[] { Dependency("org.example", "app", "[1.0.0]") },
            new MavenImportedBomConstraint[0]);

        MavenDependencyResolutionResult result = operations.ResolveDependenciesWithManagement(
            new MavenDependency[] { unmanagedRoot },
            management,
            new MavenDependencyModel[] { appTwo, appOne, runtime, transitive });

        Assert.That(result.conflicts, Is.Empty);
        AssertNode(result, "app", "1.0.0", 0, true, MavenDependencyScope.Compile);
        AssertNode(result, "runtime", "1.0.0", 1, false, MavenDependencyScope.Runtime);
        AssertNode(result, "transitive", "1.0.0", 2, false, MavenDependencyScope.Runtime);
    }

    [Test]
    public void DiagnosticFailures_CoverVersionsRangesMetadataSnapshotsAndArtifacts()
    {
        IMavenResolution operations = new MavenOperations();

        VersionParseResult missingVersion = operations.ParseVersion("");
        VersionParseResult invalidVersion = operations.ParseVersion("bad version");
        VersionRangeParseResult missingRange = operations.ParseRange("  ");
        VersionRangeParseResult invalidRange = operations.ParseRange("[2.0,1.0]");
        MavenMetadataParseResult nullMetadata = operations.ParseMetadata(null!);
        MavenMetadataParseResult wrongRoot = operations.ParseMetadata("<project />");
        MavenMetadataParseResult missingSnapshotVersion = operations.ParseMetadata(
            SnapshotWithoutRootVersionXml());

        AssertDiagnostic(missingVersion.diagnostics, MavenDiagnosticCodes.MissingVersion);
        AssertDiagnostic(invalidVersion.diagnostics, MavenDiagnosticCodes.InvalidVersion);
        AssertDiagnostic(missingRange.diagnostics, MavenRangeDiagnosticCodes.MissingRange);
        AssertDiagnostic(invalidRange.diagnostics, MavenRangeDiagnosticCodes.InvalidRange);
        AssertDiagnostic(nullMetadata.diagnostics, MavenMetadataDiagnosticCodes.MalformedMetadata);
        AssertDiagnostic(wrongRoot.diagnostics, MavenMetadataDiagnosticCodes.MalformedMetadata);
        AssertDiagnostic(
            missingSnapshotVersion.diagnostics,
            MavenMetadataDiagnosticCodes.MissingSnapshotMetadata);

        MavenMetadataParseResult entries = operations.ParseMetadata(SnapshotEntriesXml());
        Assert.That(entries.success, Is.True);
        MavenArtifactVersionResolutionResult missingEntry =
            operations.ResolveSnapshotVersionWithDiagnostics(entries.metadata!, "war", null);
        Assert.That(missingEntry.success, Is.False);
        AssertDiagnostic(
            missingEntry.diagnostics,
            MavenMetadataDiagnosticCodes.MissingSnapshotMetadata);

        MavenArtifactVersionIdentity snapshotIdentity = Identity(
            operations,
            "org.example",
            "snapshot-demo",
            "jar",
            null,
            "1.5-SNAPSHOT");
        MavenSnapshotMetadata snapshot = new MavenSnapshotMetadata(
            snapshotIdentity,
            "20250102.030405",
            8,
            "",
            false);
        MavenArtifactMetadata inconsistent = new MavenArtifactMetadata(
            snapshotIdentity,
            new MavenRepositoryId("central"),
            snapshot);
        inconsistent.identity = Identity(
            operations,
            "org.example",
            "other",
            "jar",
            null,
            "1.5-SNAPSHOT");
        MavenArtifactVersionResolutionResult inconsistentResult =
            operations.ResolveArtifactVersion(inconsistent);
        Assert.That(inconsistentResult.success, Is.False);
        AssertDiagnostic(
            inconsistentResult.diagnostics,
            MavenMetadataDiagnosticCodes.MissingSnapshotMetadata);

        Assert.That(
            delegate { operations.NormalizeVersion("bad version"); },
            Throws.TypeOf<System.ArgumentException>());
        Assert.That(
            delegate
            {
                operations.ResolveHighestRangeVersion(
                    RepositoryMetadata("org.example", "library", new string[] { "1.0" }),
                    "1.0");
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void ResolverDiagnostics_CoverMissingCoordinatesConflictingRangesAndUnsupportedScopes()
    {
        IMavenResolution operations = new MavenOperations();
        MavenDependencyModel available = Model(
            operations,
            "org.example",
            "library",
            "1.0.0",
            new MavenDependency[0]);
        MavenDependency systemRoot = new MavenDependency(
            Coordinate("org.example", "system", "jar", null),
            "[1.0.0]",
            MavenDependencyScope.System,
            false,
            new MavenDependencyExclusion[0]);

        MavenDependencyResolutionResult missing = operations.ResolveDependencies(
            new MavenDependency[] { Dependency("org.example", "missing", "[1.0.0]") },
            new MavenDependencyModel[0]);
        MavenDependencyResolutionResult conflicting = operations.ResolveDependencies(
            new MavenDependency[] { Dependency("org.example", "library", "[2.0.0]") },
            new MavenDependencyModel[] { available });
        MavenDependencyResolutionResult unsupported = operations.ResolveDependencies(
            new MavenDependency[] { systemRoot },
            new MavenDependencyModel[]
            {
                Model(operations, "org.example", "system", "1.0.0", new MavenDependency[0])
            });

        AssertConflict(missing, MavenResolverDiagnosticCodes.UnresolvedArtifactCoordinate);
        AssertConflict(conflicting, MavenResolverDiagnosticCodes.ConflictingRanges);
        AssertConflict(unsupported, MavenResolverDiagnosticCodes.UnsupportedFeature);

        Assert.That(
            delegate
            {
                operations.ResolveDependencies(
                    new MavenDependency[] { null! },
                    new MavenDependencyModel[0]);
            },
            Throws.TypeOf<System.ArgumentException>());
        Assert.That(
            delegate
            {
                operations.ResolveDependencies(
                    new MavenDependency[0],
                    new MavenDependencyModel[] { null! });
            },
            Throws.TypeOf<System.ArgumentException>());
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

    static MavenRepositoryMetadata RepositoryMetadata(
        string groupId,
        string artifactId,
        string[] versions)
    {
        return new MavenRepositoryMetadata(
            new MavenGroupId(groupId),
            new MavenArtifactId(artifactId),
            null,
            "",
            "",
            versions,
            "",
            null,
            new MavenSnapshotVersionMetadata[0]);
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

    static MavenDependency ScopedDependency(string artifactId, MavenDependencyScope scope)
    {
        return new MavenDependency(
            Coordinate("org.example", artifactId, "jar", null),
            "[1.0.0]",
            scope,
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

    static MavenDependencyGraphNode? FindNode(
        MavenDependencyResolutionResult result,
        string artifactId)
    {
        int index = 0;

        while (index < result.nodes.Length)
        {
            if (result.nodes[index].model.identity.coordinate.artifactId.value == artifactId)
            {
                return result.nodes[index];
            }

            index = index + 1;
        }

        return null;
    }

    static void AssertNode(
        MavenDependencyResolutionResult result,
        string artifactId,
        string versionText,
        int depth,
        bool direct,
        MavenDependencyScope scope)
    {
        MavenDependencyGraphNode? node = FindNode(result, artifactId);
        Assert.That(node, Is.Not.Null, artifactId);
        Assert.That(node!.model.identity.version.originalText, Is.EqualTo(versionText), artifactId);
        Assert.That(node.depth, Is.EqualTo(depth), artifactId);
        Assert.That(node.direct, Is.EqualTo(direct), artifactId);
        Assert.That(node.effectiveScope, Is.EqualTo(scope), artifactId);
    }

    static void AssertVersion(MavenArtifactVersionIdentity? identity, string versionText)
    {
        Assert.That(identity, Is.Not.Null);
        Assert.That(identity!.version.originalText, Is.EqualTo(versionText));
    }

    static void AssertDiagnostic(VersionDiagnostic[] diagnostics, string code)
    {
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].code, Is.EqualTo(code));
        Assert.That(diagnostics[0].message, Is.Not.Empty);
    }

    static void AssertConflict(MavenDependencyResolutionResult result, string code)
    {
        Assert.That(result.conflicts, Has.Length.EqualTo(1));
        Assert.That(result.conflicts[0].diagnosticCode, Is.EqualTo(code));
        Assert.That(result.conflicts[0].message, Is.Not.Empty);
    }

    static string ReleaseMetadataXml()
    {
        return @"<metadata xmlns=""https://maven.apache.org/METADATA/1.1.0"">
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
</metadata>";
    }

    static string SnapshotEntriesXml()
    {
        return @"<metadata>
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
      <snapshotVersion>
        <extension>pom</extension>
        <value>1.5-20250102.030405-8</value>
      </snapshotVersion>
    </snapshotVersions>
  </versioning>
</metadata>";
    }

    static string SnapshotFallbackXml()
    {
        return @"<metadata>
  <groupId>org.example</groupId>
  <artifactId>snapshot-demo</artifactId>
  <version>1.5-SNAPSHOT</version>
  <versioning>
    <snapshot>
      <timestamp>20250102.030405</timestamp>
      <buildNumber>8</buildNumber>
    </snapshot>
  </versioning>
</metadata>";
    }

    static string LocalSnapshotXml()
    {
        return @"<metadata>
  <groupId>org.example</groupId>
  <artifactId>snapshot-demo</artifactId>
  <version>1.5-SNAPSHOT</version>
  <versioning>
    <snapshot>
      <localCopy>true</localCopy>
    </snapshot>
  </versioning>
</metadata>";
    }

    static string SnapshotWithoutRootVersionXml()
    {
        return @"<metadata>
  <groupId>org.example</groupId>
  <artifactId>snapshot-demo</artifactId>
  <versioning>
    <snapshot>
      <timestamp>20250102.030405</timestamp>
      <buildNumber>8</buildNumber>
    </snapshot>
  </versioning>
</metadata>";
    }
}
