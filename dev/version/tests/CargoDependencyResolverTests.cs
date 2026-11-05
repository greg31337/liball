using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class CargoDependencyResolverTests
{
    [Test]
    public void ResolveDependencies_CreatesGraphNodesAndEdgesForTransitiveDependencies()
    {
        CargoPackageMetadata packageA = Package(
            "a",
            "1.0.0",
            new CargoDependency[] { CargoDependency.Create("b", "=1.0.0") },
            new CargoFeature[0]);
        CargoPackageMetadata packageB = Package("b", "1.0.0", new CargoDependency[0], new CargoFeature[0]);

        CargoDependencyResolutionResult result = CargoVersions.ResolveDependencies(
            new CargoDependency[] { CargoDependency.Create("a", "=1.0.0") },
            new CargoPackageMetadata[] { packageA, packageB },
            CargoResolverOptions.CreateDefault());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetNodeCount(), Is.EqualTo(2));
        AssertSelected(result, "a", "1.0.0", 0, true);
        AssertSelected(result, "b", "1.0.0", 1, false);
        Assert.That(result.GetEdgeCount(), Is.EqualTo(2));
        Assert.That(result.GetEdge(0).HasParent(), Is.False);
        Assert.That(result.GetEdge(1).HasParent(), Is.True);
        Assert.That(result.GetEdge(1).GetParentIdentity().GetPackageName().MatchesText("a"), Is.True);
        Assert.That(result.GetEdge(1).GetDependency().GetPackageName().MatchesText("b"), Is.True);
    }

    [Test]
    public void ResolveDependencies_BacktracksToLowerCandidateWhenHighestCandidateFailsTransitively()
    {
        CargoPackageMetadata packageA1 = Package(
            "a",
            "1.0.0",
            new CargoDependency[] { CargoDependency.Create("b", "=1.0.0") },
            new CargoFeature[0]);
        CargoPackageMetadata packageA2 = Package(
            "a",
            "2.0.0",
            new CargoDependency[] { CargoDependency.Create("b", "=2.0.0") },
            new CargoFeature[0]);
        CargoPackageMetadata packageB = Package("b", "1.0.0", new CargoDependency[0], new CargoFeature[0]);

        CargoDependencyResolutionResult result = CargoVersions.ResolveDependencies(
            new CargoDependency[] { CargoDependency.Create("a", ">=1.0.0, <3.0.0") },
            new CargoPackageMetadata[] { packageA1, packageA2, packageB },
            CargoResolverOptions.CreateDefault());

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "a", "1.0.0", 0, true);
        AssertSelected(result, "b", "1.0.0", 1, false);
    }

    [Test]
    public void ResolveDependencies_ReportsConflictForDisjointTransitiveConstraints()
    {
        CargoPackageMetadata packageA = Package(
            "a",
            "1.0.0",
            new CargoDependency[] { CargoDependency.Create("c", "=1.0.0") },
            new CargoFeature[0]);
        CargoPackageMetadata packageB = Package(
            "b",
            "1.0.0",
            new CargoDependency[] { CargoDependency.Create("c", "=2.0.0") },
            new CargoFeature[0]);
        CargoPackageMetadata packageC1 = Package("c", "1.0.0", new CargoDependency[0], new CargoFeature[0]);
        CargoPackageMetadata packageC2 = Package("c", "2.0.0", new CargoDependency[0], new CargoFeature[0]);

        CargoDependencyResolutionResult result = CargoVersions.ResolveDependencies(
            new CargoDependency[]
            {
                CargoDependency.Create("a", "=1.0.0"),
                CargoDependency.Create("b", "=1.0.0")
            },
            new CargoPackageMetadata[] { packageA, packageB, packageC1, packageC2 },
            CargoResolverOptions.CreateDefault());

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetConflictCount(), Is.EqualTo(1));
        Assert.That(result.GetConflict(0).GetPackageName().MatchesText("c"), Is.True);
        Assert.That(result.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(CargoResolverDiagnosticCodes.UnsatisfiedConstraints));
    }

    [Test]
    public void ResolveDependencies_ReportsMissingPackageMetadata()
    {
        CargoDependencyResolutionResult result = CargoVersions.ResolveDependencies(
            new CargoDependency[] { CargoDependency.Create("missing", "1") },
            new CargoPackageMetadata[0],
            CargoResolverOptions.CreateDefault());

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetConflictCount(), Is.EqualTo(1));
        Assert.That(result.GetConflict(0).GetPackageName().MatchesText("missing"), Is.True);
        Assert.That(result.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(CargoResolverDiagnosticCodes.MissingPackageMetadata));
    }

    [Test]
    public void ResolveDependencies_ReportsCyclicDependency()
    {
        CargoPackageMetadata packageA = Package(
            "a",
            "1.0.0",
            new CargoDependency[] { CargoDependency.Create("b", "=1.0.0") },
            new CargoFeature[0]);
        CargoPackageMetadata packageB = Package(
            "b",
            "1.0.0",
            new CargoDependency[] { CargoDependency.Create("a", "=1.0.0") },
            new CargoFeature[0]);

        CargoDependencyResolutionResult result = CargoVersions.ResolveDependencies(
            new CargoDependency[] { CargoDependency.Create("a", "=1.0.0") },
            new CargoPackageMetadata[] { packageA, packageB },
            CargoResolverOptions.CreateDefault());

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetConflict(0).GetPackageName().MatchesText("a"), Is.True);
        Assert.That(result.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(CargoResolverDiagnosticCodes.CyclicDependency));
    }

    [Test]
    public void ResolveDependencies_StopsWhenBacktrackingLimitIsExceeded()
    {
        CargoPackageMetadata packageA1 = Package(
            "a",
            "1.0.0",
            new CargoDependency[] { CargoDependency.Create("b", "=1.0.0") },
            new CargoFeature[0]);
        CargoPackageMetadata packageA2 = Package(
            "a",
            "2.0.0",
            new CargoDependency[] { CargoDependency.Create("b", "=2.0.0") },
            new CargoFeature[0]);
        CargoPackageMetadata packageB = Package("b", "1.0.0", new CargoDependency[0], new CargoFeature[0]);
        CargoResolverOptions options = CargoResolverOptions.CreateDefault();
        options.SetMaxBacktrackingSteps(1);

        CargoDependencyResolutionResult result = CargoVersions.ResolveDependencies(
            new CargoDependency[] { CargoDependency.Create("a", ">=1.0.0, <3.0.0") },
            new CargoPackageMetadata[] { packageA1, packageA2, packageB },
            options);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(CargoResolverDiagnosticCodes.BacktrackingLimitExceeded));
    }

    [Test]
    public void ResolveDependencies_UsesManifestDefaultFeaturesToActivateOptionalTransitiveDependencies()
    {
        CargoManifest manifest = CargoVersions.ParseManifest("""
            [dependencies]
            app = "1"
            """);
        CargoPackageMetadata app = Package(
            "app",
            "1.0.0",
            new CargoDependency[]
            {
                new CargoDependency(
                    new CargoPackageName("serde"),
                    "1",
                    CargoDependencyKind.Normal,
                    true,
                    true,
                    new string[0],
                    null,
                    null)
            },
            new CargoFeature[]
            {
                CargoFeature.Create("default", new string[] { "dep:serde", "serde/derive" })
            });
        CargoPackageMetadata serde = Package(
            "serde",
            "1.0.0",
            new CargoDependency[0],
            new CargoFeature[] { CargoFeature.Create("derive", new string[0]) });

        CargoDependencyResolutionResult result = CargoVersions.ResolveDependencies(
            manifest,
            new CargoPackageMetadata[] { app, serde },
            CargoResolverOptions.CreateDefault());

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "app", "1.0.0", 0, true);
        CargoDependencyGraphNode serdeNode = result.GetSelectedPackage("serde");
        Assert.That(serdeNode.GetSelectedFeatures().HasFeature("derive"), Is.True);
    }

    [Test]
    public void ResolveDependencies_RejectsNullInputElements()
    {
        Assert.That(
            delegate
            {
                CargoVersions.ResolveDependencies(
                    new CargoDependency[] { null! },
                    new CargoPackageMetadata[0],
                    CargoResolverOptions.CreateDefault());
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                CargoVersions.ResolveDependencies(
                    new CargoDependency[0],
                    new CargoPackageMetadata[] { null! },
                    CargoResolverOptions.CreateDefault());
            },
            Throws.TypeOf<ArgumentException>());
    }

    static void AssertSelected(CargoDependencyResolutionResult result, string packageName, string version, int depth, bool direct)
    {
        CargoDependencyGraphNode node = result.GetSelectedPackage(packageName);

        Assert.That(node.GetIdentity().GetPackageName().MatchesText(packageName), Is.True);
        Assert.That(node.GetIdentity().GetVersionText(), Is.EqualTo(version));
        Assert.That(node.GetDepth(), Is.EqualTo(depth));
        Assert.That(node.IsDirect(), Is.EqualTo(direct));
    }

    static CargoPackageMetadata Package(
        string packageName,
        string version,
        CargoDependency[] dependencies,
        CargoFeature[] features)
    {
        return new CargoPackageMetadata(
            CargoPackageVersionIdentity.Create(packageName, version),
            dependencies,
            features,
            CargoRegistrySource.Create("crates-io", "https://example.test/crates-io-index"),
            false);
    }
}
