using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetDependencyGraphResolverTests
{
    [Test]
    public void Resolve_CreatesGraphNodesAndEdgesForRootAndTransitiveDependencies()
    {
        NuGetSourcePackageMetadata packageA = Package(
            "A",
            "1.0.0",
            new NuGetDependency[] { NuGetDependency.Create("B", "[1.0.0]") });
        NuGetSourcePackageMetadata packageB = Package("B", "1.0.0", new NuGetDependency[0]);

        NuGetDependencyResolutionResult result = NuGetVersions.ResolveDependencies(
            new NuGetDependency[] { NuGetDependency.Create("A", "[1.0.0]") },
            new NuGetSourcePackageMetadata[] { packageA, packageB });

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetNodeCount(), Is.EqualTo(2));
        AssertSelected(result, "A", "1.0.0", 0, true);
        AssertSelected(result, "B", "1.0.0", 1, false);
        Assert.That(result.GetEdgeCount(), Is.EqualTo(2));
        Assert.That(result.GetEdge(0).HasParent(), Is.False);
        Assert.That(result.GetEdge(0).GetConstraint().IsDirect(), Is.True);
        Assert.That(result.GetEdge(1).HasParent(), Is.True);
        Assert.That(result.GetEdge(1).GetParentIdentity().GetPackageId().MatchesText("A"), Is.True);
        Assert.That(result.GetEdge(1).GetConstraint().GetPackageId().MatchesText("B"), Is.True);
    }

    [Test]
    public void Resolve_DirectDependencyWinsOverTransitiveDependency()
    {
        NuGetSourcePackageMetadata packageA = Package(
            "A",
            "1.0.0",
            new NuGetDependency[] { NuGetDependency.Create("C", "[2.0.0]") });
        NuGetSourcePackageMetadata packageC1 = Package("C", "1.0.0", new NuGetDependency[0]);
        NuGetSourcePackageMetadata packageC2 = Package("C", "2.0.0", new NuGetDependency[0]);

        NuGetDependencyResolutionResult result = NuGetDependencyGraphResolver.Resolve(
            new NuGetDependency[]
            {
                NuGetDependency.Create("A", "[1.0.0]"),
                NuGetDependency.Create("C", "[1.0.0]")
            },
            new NuGetSourcePackageMetadata[] { packageA, packageC1, packageC2 });

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "C", "1.0.0", 0, true);
    }

    [Test]
    public void Resolve_NearestDependencyWinsOverFartherTransitiveConstraint()
    {
        NuGetSourcePackageMetadata packageA = Package(
            "A",
            "1.0.0",
            new NuGetDependency[]
            {
                NuGetDependency.Create("B", "[1.0.0]"),
                NuGetDependency.Create("D", "[1.0.0]")
            });
        NuGetSourcePackageMetadata packageB = Package(
            "B",
            "1.0.0",
            new NuGetDependency[] { NuGetDependency.Create("D", "[2.0.0]") });
        NuGetSourcePackageMetadata packageD1 = Package("D", "1.0.0", new NuGetDependency[0]);
        NuGetSourcePackageMetadata packageD2 = Package("D", "2.0.0", new NuGetDependency[0]);

        NuGetDependencyResolutionResult result = NuGetDependencyGraphResolver.Resolve(
            new NuGetDependency[] { NuGetDependency.Create("A", "[1.0.0]") },
            new NuGetSourcePackageMetadata[] { packageA, packageB, packageD1, packageD2 });

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "D", "1.0.0", 1, false);
    }

    [Test]
    public void Resolve_CousinDependenciesAtSameDepthMergeCompatibleConstraints()
    {
        NuGetSourcePackageMetadata packageA = Package(
            "A",
            "1.0.0",
            new NuGetDependency[] { NuGetDependency.Create("C", "[1.0.0,3.0.0)") });
        NuGetSourcePackageMetadata packageB = Package(
            "B",
            "1.0.0",
            new NuGetDependency[] { NuGetDependency.Create("C", "[2.0.0,4.0.0)") });
        NuGetSourcePackageMetadata packageC1 = Package("C", "1.0.0", new NuGetDependency[0]);
        NuGetSourcePackageMetadata packageC2 = Package("C", "2.0.0", new NuGetDependency[0]);
        NuGetSourcePackageMetadata packageC3 = Package("C", "3.0.0", new NuGetDependency[0]);

        NuGetDependencyResolutionResult result = NuGetDependencyGraphResolver.Resolve(
            new NuGetDependency[]
            {
                NuGetDependency.Create("A", "[1.0.0]"),
                NuGetDependency.Create("B", "[1.0.0]")
            },
            new NuGetSourcePackageMetadata[] { packageA, packageB, packageC1, packageC2, packageC3 });

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "C", "2.0.0", 1, false);
    }

    [Test]
    public void Resolve_CousinDependenciesAtSameDepthReportConflictForDisjointConstraints()
    {
        NuGetSourcePackageMetadata packageA = Package(
            "A",
            "1.0.0",
            new NuGetDependency[] { NuGetDependency.Create("C", "[1.0.0]") });
        NuGetSourcePackageMetadata packageB = Package(
            "B",
            "1.0.0",
            new NuGetDependency[] { NuGetDependency.Create("C", "[2.0.0]") });
        NuGetSourcePackageMetadata packageC1 = Package("C", "1.0.0", new NuGetDependency[0]);
        NuGetSourcePackageMetadata packageC2 = Package("C", "2.0.0", new NuGetDependency[0]);

        NuGetDependencyResolutionResult result = NuGetDependencyGraphResolver.Resolve(
            new NuGetDependency[]
            {
                NuGetDependency.Create("A", "[1.0.0]"),
                NuGetDependency.Create("B", "[1.0.0]")
            },
            new NuGetSourcePackageMetadata[] { packageA, packageB, packageC1, packageC2 });

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetConflictCount(), Is.EqualTo(1));
        Assert.That(result.GetConflict(0).GetPackageId().MatchesText("C"), Is.True);
        Assert.That(result.GetConflict(0).GetDepth(), Is.EqualTo(1));
        Assert.That(result.HasSelectedPackage("C"), Is.False);
    }

    [Test]
    public void Resolve_UsesRejectionPolicyWhenSelectingPackages()
    {
        NuGetSourcePackageMetadata unavailable = Package(
            "A",
            "1.0.0",
            NuGetListedState.Listed,
            NuGetPackageAvailability.Unavailable,
            new NuGetDependency[0]);

        NuGetDependencyResolutionResult result = NuGetDependencyGraphResolver.Resolve(
            new NuGetDependency[] { NuGetDependency.Create("A", "[1.0.0]") },
            new NuGetSourcePackageMetadata[] { unavailable });

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetConflict(0).GetPackageId().MatchesText("A"), Is.True);
    }

    [Test]
    public void ResolutionResult_DefensivelyCopiesNodesEdgesAndConflicts()
    {
        NuGetDependencyResolutionResult result = NuGetDependencyGraphResolver.Resolve(
            new NuGetDependency[] { NuGetDependency.Create("A", "[1.0.0]") },
            new NuGetSourcePackageMetadata[] { Package("A", "1.0.0", new NuGetDependency[0]) });

        NuGetDependencyGraphNode[] nodes = result.GetNodes();
        NuGetDependencyGraphEdge[] edges = result.GetEdges();
        NuGetDependencyConflict[] conflicts = result.GetConflicts();

        nodes[0] = new NuGetDependencyGraphNode(Package("Other", "1.0.0", new NuGetDependency[0]), 0, true);
        edges[0] = NuGetDependencyGraphEdge.CreateRootEdge(NuGetDependency.Create("Other", "[1.0.0]"));

        Assert.That(conflicts.Length, Is.EqualTo(0));
        AssertSelected(result, "A", "1.0.0", 0, true);
        Assert.That(result.GetEdge(0).GetConstraint().GetPackageId().MatchesText("A"), Is.True);
    }

    [Test]
    public void Resolver_RejectsNullElementsInInputArrays()
    {
        Assert.That(
            delegate
            {
                NuGetDependencyGraphResolver.Resolve(
                    new NuGetDependency[] { null! },
                    new NuGetSourcePackageMetadata[0]);
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                NuGetDependencyGraphResolver.Resolve(
                    new NuGetDependency[0],
                    new NuGetSourcePackageMetadata[] { null! });
            },
            Throws.TypeOf<ArgumentException>());
    }

    static void AssertSelected(NuGetDependencyResolutionResult result, string packageId, string version, int depth, bool direct)
    {
        NuGetDependencyGraphNode node = result.GetSelectedPackage(packageId);

        Assert.That(node.GetIdentity().GetPackageId().MatchesText(packageId), Is.True);
        Assert.That(node.GetIdentity().GetNormalizedVersionText(), Is.EqualTo(version));
        Assert.That(node.GetDepth(), Is.EqualTo(depth));
        Assert.That(node.IsDirect(), Is.EqualTo(direct));
    }

    static NuGetSourcePackageMetadata Package(string packageId, string version, NuGetDependency[] dependencies)
    {
        return Package(packageId, version, NuGetListedState.Listed, NuGetPackageAvailability.Available, dependencies);
    }

    static NuGetSourcePackageMetadata Package(
        string packageId,
        string version,
        NuGetListedState listedState,
        NuGetPackageAvailability availability,
        NuGetDependency[] dependencies)
    {
        NuGetPackageMetadata packageMetadata = new NuGetPackageMetadata(
            NuGetPackageVersionIdentity.Create(packageId, version),
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            false,
            false,
            new NuGetDependencyGroup[]
            {
                NuGetDependencyGroup.CreateAnyFramework(dependencies)
            });

        return new NuGetSourcePackageMetadata(
            new NuGetSourceIdentity("source", "https://example.test/v3/index.json"),
            packageMetadata,
            listedState,
            availability,
            NuGetPackageDeprecationMetadata.CreateNotDeprecated(),
            new NuGetPackageVulnerabilityMetadata[0]);
    }
}
