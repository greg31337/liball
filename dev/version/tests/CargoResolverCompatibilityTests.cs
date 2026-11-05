using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class CargoResolverCompatibilityTests
{
    [Test]
    public void ResolveDependencies_BacktracksAcrossPublishedVersionsWhenHighestTransitiveSetFails()
    {
        CargoPackageMetadata app = Package(
            "app",
            "1.0.0",
            new CargoDependency[] { Dependency("util", ">=1.0.0, <3.0.0") },
            new CargoFeature[0]);
        CargoPackageMetadata util1 = Package(
            "util",
            "1.0.0",
            new CargoDependency[] { Dependency("helper", "=1.0.0") },
            new CargoFeature[0]);
        CargoPackageMetadata util2 = Package(
            "util",
            "2.0.0",
            new CargoDependency[] { Dependency("helper", "=2.0.0") },
            new CargoFeature[0]);
        CargoPackageMetadata helper = Package("helper", "1.0.0", new CargoDependency[0], new CargoFeature[0]);

        CargoDependencyResolutionResult result = CargoVersions.ResolveDependencies(
            new CargoDependency[] { Dependency("app", "=1.0.0") },
            new CargoPackageMetadata[] { app, util1, util2, helper },
            CargoResolverOptions.CreateDefault());

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "app", "1.0.0");
        AssertSelected(result, "util", "1.0.0");
        AssertSelected(result, "helper", "1.0.0");
    }

    [Test]
    public void ResolveDependencies_UnifiesFeaturesAndActivatesOptionalDependenciesFromPackageFeatures()
    {
        CargoManifest rootManifest = CargoVersions.ParseManifest("""
            [dependencies]
            app = { version = "1", features = ["serde-support"] }
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
                    false,
                    new string[] { "alloc" },
                    null,
                    null)
            },
            new CargoFeature[]
            {
                CargoFeature.Create("serde-support", new string[] { "dep:serde", "serde/derive" })
            });
        CargoPackageMetadata serde = Package(
            "serde",
            "1.0.0",
            new CargoDependency[0],
            new CargoFeature[]
            {
                CargoFeature.Create("alloc", new string[0]),
                CargoFeature.Create("derive", new string[0])
            });

        CargoDependencyResolutionResult result = CargoVersions.ResolveDependencies(
            rootManifest,
            new CargoPackageMetadata[] { app, serde },
            CargoResolverOptions.CreateDefault());

        Assert.That(result.IsSuccess(), Is.True);
        CargoDependencyGraphNode serdeNode = result.GetSelectedPackage("serde");
        Assert.That(serdeNode.GetSelectedFeatures().UsesDefaultFeatures(), Is.False);
        Assert.That(serdeNode.GetSelectedFeatures().HasFeature("alloc"), Is.True);
        Assert.That(serdeNode.GetSelectedFeatures().HasFeature("derive"), Is.True);
    }

    [Test]
    public void ResolveDependencies_FiltersYankedAndPrereleaseCandidatesUnlessOptionsAllowThem()
    {
        CargoPackageMetadata stable = Package("lib", "1.0.0", new CargoDependency[0], new CargoFeature[0]);
        CargoPackageMetadata prerelease = Package("lib", "1.1.0-alpha.1", new CargoDependency[0], new CargoFeature[0]);
        CargoPackageMetadata yanked = YankedPackage("lib", "1.2.0", new CargoDependency[0], new CargoFeature[0]);
        CargoResolverOptions includeAll = CargoResolverOptions.CreateDefault();
        includeAll.SetIncludePrereleaseVersions(true);
        includeAll.SetIncludeYankedVersions(true);

        CargoDependencyResolutionResult defaultResult = CargoVersions.ResolveDependencies(
            new CargoDependency[] { Dependency("lib", ">=1.0.0, <2.0.0") },
            new CargoPackageMetadata[] { stable, prerelease, yanked },
            CargoResolverOptions.CreateDefault());
        CargoDependencyResolutionResult includeAllResult = CargoVersions.ResolveDependencies(
            new CargoDependency[] { Dependency("lib", ">=1.0.0, <2.0.0") },
            new CargoPackageMetadata[] { stable, prerelease, yanked },
            includeAll);

        Assert.That(defaultResult.IsSuccess(), Is.True);
        AssertSelected(defaultResult, "lib", "1.0.0");
        Assert.That(includeAllResult.IsSuccess(), Is.True);
        AssertSelected(includeAllResult, "lib", "1.2.0");
    }

    [Test]
    public void ResolveDependencies_HonorsAlternateRegistryNamesAndSourceReplacement()
    {
        CargoDependency privateDependency = new CargoDependency(
            new CargoPackageName("private-lib"),
            "1",
            CargoDependencyKind.Normal,
            false,
            true,
            new string[0],
            "private",
            null);
        CargoPackageMetadata mirrorOnly = PackageWithSource(
            "mirror-only",
            "1.0.0",
            new CargoDependency[0],
            new CargoFeature[0],
            "mirror",
            "https://example.test/mirror-index",
            false);
        CargoPackageMetadata wrongSource = PackageWithSource(
            "mirror-only",
            "1.1.0",
            new CargoDependency[0],
            new CargoFeature[0],
            "crates-io",
            "https://example.test/crates-io-index",
            false);
        CargoPackageMetadata privatePackage = PackageWithSource(
            "private-lib",
            "1.0.0",
            new CargoDependency[0],
            new CargoFeature[0],
            "private",
            "https://example.test/private-index",
            false);
        CargoResolverOptions options = CargoResolverOptions.CreateDefault();
        options.SetRegistrySources(new CargoRegistrySource[]
        {
            CargoRegistrySource.Create("private", "https://example.test/private-index")
        });
        options.SetSourceReplacements(new CargoSourceReplacement[]
        {
            CargoSourceReplacement.Create(
                "crates-io",
                CargoRegistrySource.Create("mirror", "https://example.test/mirror-index"))
        });

        CargoDependencyResolutionResult result = CargoVersions.ResolveDependencies(
            new CargoDependency[] { Dependency("mirror-only", "1"), privateDependency },
            new CargoPackageMetadata[] { mirrorOnly, wrongSource, privatePackage },
            options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetSelectedPackage("mirror-only").GetPackageMetadata().GetRegistrySource().GetName(), Is.EqualTo("mirror"));
        Assert.That(result.GetSelectedPackage("private-lib").GetPackageMetadata().GetRegistrySource().GetName(), Is.EqualTo("private"));
    }

    [Test]
    public void ResolveDependencies_FiltersTargetSpecificDependenciesForConfiguredTarget()
    {
        CargoManifest appManifest = CargoVersions.ParseManifest("""
            [dependencies]
            base = "1"

            [target.'cfg(windows)'.dependencies]
            winapi = "1"

            [target.'cfg(unix)'.dependencies]
            libc = "1"
            """);
        CargoPackageMetadata app = Package("app", "1.0.0", appManifest.GetDependencies(), appManifest.GetFeatures());
        CargoPackageMetadata basePackage = Package("base", "1.0.0", new CargoDependency[0], new CargoFeature[0]);
        CargoPackageMetadata libc = Package("libc", "1.0.0", new CargoDependency[0], new CargoFeature[0]);
        CargoPackageMetadata winapi = Package("winapi", "1.0.0", new CargoDependency[0], new CargoFeature[0]);
        CargoResolverOptions options = CargoResolverOptions.CreateDefault();
        options.SetResolverVersion(CargoResolverVersion.Version2);
        options.SetTargetTriple("x86_64-unknown-linux-gnu");

        CargoDependencyResolutionResult result = CargoVersions.ResolveDependencies(
            new CargoDependency[] { Dependency("app", "1") },
            new CargoPackageMetadata[] { app, basePackage, libc, winapi },
            options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasSelectedPackage("base"), Is.True);
        Assert.That(result.HasSelectedPackage("libc"), Is.True);
        Assert.That(result.HasSelectedPackage("winapi"), Is.False);
    }

    static CargoDependency Dependency(string packageName, string versionRequirement)
    {
        return CargoDependency.Create(packageName, versionRequirement);
    }

    static CargoPackageMetadata Package(
        string packageName,
        string version,
        CargoDependency[] dependencies,
        CargoFeature[] features)
    {
        return PackageWithSource(
            packageName,
            version,
            dependencies,
            features,
            "crates-io",
            "https://example.test/crates-io-index",
            false);
    }

    static CargoPackageMetadata YankedPackage(
        string packageName,
        string version,
        CargoDependency[] dependencies,
        CargoFeature[] features)
    {
        return PackageWithSource(
            packageName,
            version,
            dependencies,
            features,
            "crates-io",
            "https://example.test/crates-io-index",
            true);
    }

    static CargoPackageMetadata PackageWithSource(
        string packageName,
        string version,
        CargoDependency[] dependencies,
        CargoFeature[] features,
        string sourceName,
        string sourceUrl,
        bool yanked)
    {
        return new CargoPackageMetadata(
            CargoPackageVersionIdentity.Create(packageName, version),
            dependencies,
            features,
            CargoRegistrySource.Create(sourceName, sourceUrl),
            yanked);
    }

    static void AssertSelected(CargoDependencyResolutionResult result, string packageName, string version)
    {
        CargoDependencyGraphNode node = result.GetSelectedPackage(packageName);

        Assert.That(node.GetIdentity().GetPackageName().MatchesText(packageName), Is.True);
        Assert.That(node.GetIdentity().GetVersionText(), Is.EqualTo(version));
    }
}
