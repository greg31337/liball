using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenDependencyResolverTests
{
    [Test]
    public void Resolve_CreatesGraphNodesAndEdgesForRootAndTransitiveDependencies()
    {
        MavenDependencyModel packageA = Model("com.example", "a", "1.0.0", new MavenDependency[] { Dep("com.example", "b", "[1.0.0]") });
        MavenDependencyModel packageB = Model("com.example", "b", "1.0.0", new MavenDependency[0]);

        MavenDependencyResolutionResult result = MavenVersions.ResolveDependencies(
            new MavenDependency[] { Dep("com.example", "a", "[1.0.0]") },
            new MavenDependencyModel[] { packageA, packageB });

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetNodeCount(), Is.EqualTo(2));
        AssertSelected(result, "a", "1.0.0", 0, true, MavenDependencyScope.Compile);
        AssertSelected(result, "b", "1.0.0", 1, false, MavenDependencyScope.Compile);
        Assert.That(result.GetEdgeCount(), Is.EqualTo(2));
        Assert.That(result.GetEdge(0).HasParent(), Is.False);
        Assert.That(result.GetEdge(1).HasParent(), Is.True);
        Assert.That(result.GetEdge(1).GetParentIdentity().GetArtifactId().MatchesText("a"), Is.True);
    }

    [Test]
    public void Resolve_NearestDependencyWinsOverFartherTransitiveDependency()
    {
        MavenDependencyModel packageA = Model(
            "com.example",
            "a",
            "1.0.0",
            new MavenDependency[] { Dep("com.example", "b", "[1.0.0]"), Dep("com.example", "c", "[1.0.0]") });
        MavenDependencyModel packageB = Model(
            "com.example",
            "b",
            "1.0.0",
            new MavenDependency[] { Dep("com.example", "c", "[2.0.0]") });
        MavenDependencyModel packageC1 = Model("com.example", "c", "1.0.0", new MavenDependency[0]);
        MavenDependencyModel packageC2 = Model("com.example", "c", "2.0.0", new MavenDependency[0]);

        MavenDependencyResolutionResult result = MavenDependencyResolver.Resolve(
            new MavenDependency[] { Dep("com.example", "a", "[1.0.0]") },
            new MavenDependencyModel[] { packageA, packageB, packageC1, packageC2 });

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "c", "1.0.0", 1, false, MavenDependencyScope.Compile);
    }

    [Test]
    public void Resolve_AppliesDependencyManagementVersionToUnversionedDependencies()
    {
        MavenDependency managedVersion = Dep("com.example", "managed", "[2.0.0]");
        MavenDependencyModel packageA = ModelWithManagement(
            "com.example",
            "a",
            "1.0.0",
            new MavenDependency[] { MavenDependency.CreateUnversioned("com.example", "managed") },
            new MavenDependencyManagement(new MavenDependency[] { managedVersion }, new MavenImportedBomConstraint[0]));
        MavenDependencyModel managed1 = Model("com.example", "managed", "1.0.0", new MavenDependency[0]);
        MavenDependencyModel managed2 = Model("com.example", "managed", "2.0.0", new MavenDependency[0]);

        MavenDependencyResolutionResult result = MavenVersions.ResolveDependencies(
            new MavenDependency[] { Dep("com.example", "a", "[1.0.0]") },
            new MavenDependencyModel[] { packageA, managed1, managed2 });

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "managed", "2.0.0", 1, false, MavenDependencyScope.Compile);
    }

    [Test]
    public void Resolve_InheritsParentManagementAndImportsBomManagement()
    {
        MavenDependencyModel parent = PomModelWithManagement(
            "com.example",
            "parent",
            "1.0.0",
            new MavenDependencyManagement(
                new MavenDependency[] { Dep("com.example", "managed", "[1.0.0]") },
                new MavenImportedBomConstraint[0]));
        MavenDependencyModel bom = PomModelWithManagement(
            "com.example",
            "bom",
            "1.0.0",
            new MavenDependencyManagement(
                new MavenDependency[] { Dep("com.example", "bom-managed", "[2.0.0]") },
                new MavenImportedBomConstraint[0]));
        MavenDependencyModel app = ModelWithParentAndManagement(
            "com.example",
            "app",
            "1.0.0",
            MavenParentReference.Create("com.example", "parent", "1.0.0"),
            new MavenDependency[]
            {
                MavenDependency.CreateUnversioned("com.example", "managed"),
                MavenDependency.CreateUnversioned("com.example", "bom-managed")
            },
            new MavenDependencyManagement(
                new MavenDependency[] { Dep("com.example", "managed", "[3.0.0]") },
                new MavenImportedBomConstraint[] { MavenImportedBomConstraint.Create("com.example", "bom", "1.0.0") }));
        MavenDependencyModel managed1 = Model("com.example", "managed", "1.0.0", new MavenDependency[0]);
        MavenDependencyModel managed3 = Model("com.example", "managed", "3.0.0", new MavenDependency[0]);
        MavenDependencyModel bomManaged = Model("com.example", "bom-managed", "2.0.0", new MavenDependency[0]);

        MavenDependencyResolutionResult result = MavenVersions.ResolveDependencies(
            new MavenDependency[] { Dep("com.example", "app", "[1.0.0]") },
            new MavenDependencyModel[] { app, parent, bom, managed1, managed3, bomManaged });

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "managed", "3.0.0", 1, false, MavenDependencyScope.Compile);
        AssertSelected(result, "bom-managed", "2.0.0", 1, false, MavenDependencyScope.Compile);
    }

    [Test]
    public void Resolve_HonorsExclusionsOptionalPropagationAndOmittedTransitiveScopes()
    {
        MavenDependency dependencyOnB = new MavenDependency(
            MavenArtifactCoordinate.CreateJar("com.example", "b"),
            "[1.0.0]",
            MavenDependencyScope.Compile,
            false,
            new MavenDependencyExclusion[] { MavenDependencyExclusion.Create("com.example", "c") });
        MavenDependency optionalD = new MavenDependency(
            MavenArtifactCoordinate.CreateJar("com.example", "d"),
            "[1.0.0]",
            MavenDependencyScope.Compile,
            true,
            new MavenDependencyExclusion[0]);
        MavenDependency providedE = new MavenDependency(
            MavenArtifactCoordinate.CreateJar("com.example", "e"),
            "[1.0.0]",
            MavenDependencyScope.Provided,
            false,
            new MavenDependencyExclusion[0]);
        MavenDependencyModel packageA = Model("com.example", "a", "1.0.0", new MavenDependency[] { dependencyOnB });
        MavenDependencyModel packageB = Model(
            "com.example",
            "b",
            "1.0.0",
            new MavenDependency[] { Dep("com.example", "c", "[1.0.0]"), optionalD, providedE });

        MavenDependencyResolutionResult result = MavenDependencyResolver.Resolve(
            new MavenDependency[] { Dep("com.example", "a", "[1.0.0]") },
            new MavenDependencyModel[]
            {
                packageA,
                packageB,
                Model("com.example", "c", "1.0.0", new MavenDependency[0]),
                Model("com.example", "d", "1.0.0", new MavenDependency[0]),
                Model("com.example", "e", "1.0.0", new MavenDependency[0])
            });

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasSelectedArtifact("com.example", "b"), Is.True);
        Assert.That(result.HasSelectedArtifact("com.example", "c"), Is.False);
        Assert.That(result.HasSelectedArtifact("com.example", "d"), Is.False);
        Assert.That(result.HasSelectedArtifact("com.example", "e"), Is.False);
    }

    [Test]
    public void Resolve_CombinesRuntimeScopeTransitively()
    {
        MavenDependency runtimeB = new MavenDependency(
            MavenArtifactCoordinate.CreateJar("com.example", "b"),
            "[1.0.0]",
            MavenDependencyScope.Runtime,
            false,
            new MavenDependencyExclusion[0]);
        MavenDependencyModel packageA = Model("com.example", "a", "1.0.0", new MavenDependency[] { runtimeB });
        MavenDependencyModel packageB = Model("com.example", "b", "1.0.0", new MavenDependency[] { Dep("com.example", "c", "[1.0.0]") });
        MavenDependencyModel packageC = Model("com.example", "c", "1.0.0", new MavenDependency[0]);

        MavenDependencyResolutionResult result = MavenVersions.ResolveDependencies(
            new MavenDependency[] { Dep("com.example", "a", "[1.0.0]") },
            new MavenDependencyModel[] { packageA, packageB, packageC });

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "b", "1.0.0", 1, false, MavenDependencyScope.Runtime);
        AssertSelected(result, "c", "1.0.0", 2, false, MavenDependencyScope.Runtime);
    }

    [Test]
    public void Resolve_ReportsConflictForMissingArtifactAndRejectsNullArrays()
    {
        MavenDependencyResolutionResult result = MavenDependencyResolver.Resolve(
            new MavenDependency[] { Dep("com.example", "missing", "[1.0.0]") },
            new MavenDependencyModel[0]);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetConflictCount(), Is.EqualTo(1));
        Assert.That(result.GetConflict(0).GetCoordinate().GetArtifactId().MatchesText("missing"), Is.True);

        Assert.That(
            delegate
            {
                MavenDependencyResolver.Resolve(new MavenDependency[] { null! }, new MavenDependencyModel[0]);
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                MavenDependencyResolver.Resolve(new MavenDependency[0], new MavenDependencyModel[] { null! });
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    static MavenDependency Dep(string groupId, string artifactId, string versionRange)
    {
        return MavenDependency.Create(groupId, artifactId, versionRange);
    }

    static MavenDependencyModel Model(string groupId, string artifactId, string version, MavenDependency[] dependencies)
    {
        return ModelWithManagement(groupId, artifactId, version, dependencies, MavenDependencyManagement.CreateEmpty());
    }

    static MavenDependencyModel ModelWithManagement(
        string groupId,
        string artifactId,
        string version,
        MavenDependency[] dependencies,
        MavenDependencyManagement management)
    {
        return new MavenDependencyModel(
            MavenArtifactVersionIdentity.Create(groupId, artifactId, "jar", null, version),
            null,
            dependencies,
            management);
    }

    static MavenDependencyModel ModelWithParentAndManagement(
        string groupId,
        string artifactId,
        string version,
        MavenParentReference parent,
        MavenDependency[] dependencies,
        MavenDependencyManagement management)
    {
        return new MavenDependencyModel(
            MavenArtifactVersionIdentity.Create(groupId, artifactId, "jar", null, version),
            parent,
            dependencies,
            management);
    }

    static MavenDependencyModel PomModelWithManagement(
        string groupId,
        string artifactId,
        string version,
        MavenDependencyManagement management)
    {
        return new MavenDependencyModel(
            MavenArtifactVersionIdentity.Create(groupId, artifactId, "pom", null, version),
            null,
            new MavenDependency[0],
            management);
    }

    static void AssertSelected(
        MavenDependencyResolutionResult result,
        string artifactId,
        string version,
        int depth,
        bool direct,
        MavenDependencyScope scope)
    {
        MavenDependencyGraphNode node = result.GetSelectedArtifact("com.example", artifactId);

        Assert.That(node.GetIdentity().GetArtifactId().MatchesText(artifactId), Is.True);
        Assert.That(node.GetIdentity().GetVersionText(), Is.EqualTo(version));
        Assert.That(node.GetDepth(), Is.EqualTo(depth));
        Assert.That(node.IsDirect(), Is.EqualTo(direct));
        Assert.That(node.GetEffectiveScope(), Is.EqualTo(scope));
    }
}
