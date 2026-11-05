using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class CargoOperationsTests
{
    [Test]
    public void VersionAndRequirementOperations_ParseAndCompareCargoSemantics()
    {
        ICargoResolution operations = new CargoOperations();

        VersionParseResult version = operations.ParseVersion("1.2.3-alpha.1+build.5");
        VersionParseResult invalidVersion = operations.ParseVersion("v1.2.3");
        VersionRangeParseResult caret = operations.ParseRequirement("1.2.3");
        VersionRangeParseResult intersection = operations.ParseRequirement(">=1.2, <2");
        VersionRangeParseResult invalidRequirement = operations.ParseRequirement("=>1.2");

        Assert.That(version.success, Is.True);
        Assert.That(version.dialect, Is.EqualTo(VersionDialect.Cargo));
        Assert.That(version.version, Is.Not.Null);
        Assert.That(invalidVersion.success, Is.False);
        Assert.That(caret.success, Is.True);
        Assert.That(caret.syntax, Is.EqualTo(RangeSyntax.Cargo));
        Assert.That(intersection.success, Is.True);
        Assert.That(intersection.range, Is.Not.Null);
        Assert.That(intersection.range!.sets, Has.Length.EqualTo(1));
        Assert.That(intersection.range.sets[0].comparators, Has.Length.EqualTo(2));
        Assert.That(invalidRequirement.success, Is.False);
        Assert.That(operations.CompareVersions("1.2.3-alpha.1", "1.2.3"), Is.LessThan(0));
        Assert.That(
            operations.CompareVersions("1.2.3+build.1", "1.2.3+build.2"),
            Is.Zero);
    }

    [Test]
    public void CandidateOperations_ApplyRequirementsPrereleaseYankedAndSourcePolicies()
    {
        ICargoResolution operations = new CargoOperations();
        CargoDependency dependency = Dependency(">=1.0.0, <2.0.0");
        CargoPackageMetadata stable = Package(
            operations,
            "demo",
            "1.0.0",
            "crates-io",
            false,
            new string[0]);
        CargoPackageMetadata prerelease = Package(
            operations,
            "demo",
            "1.1.0-alpha.1",
            "crates-io",
            false,
            new string[0]);
        CargoPackageMetadata yanked = Package(
            operations,
            "demo",
            "1.2.0",
            "crates-io",
            true,
            new string[0]);
        CargoPackageMetadata mirror = Package(
            operations,
            "demo",
            "1.3.0",
            "mirror",
            false,
            new string[0]);
        CargoPackageMetadata[] candidates = new CargoPackageMetadata[]
        {
            stable,
            prerelease,
            yanked,
            mirror
        };
        CargoResolverOptions defaults = new CargoResolverOptions();

        CargoPackageMetadata[] filtered = operations.FilterCandidates(
            dependency,
            candidates,
            defaults);

        Assert.That(filtered, Is.EqualTo(new CargoPackageMetadata[] { stable }));
        Assert.That(
            operations.SelectHighestCandidate(dependency, candidates, defaults),
            Is.SameAs(stable));

        CargoResolverOptions includeAll = new CargoResolverOptions();
        includeAll.includePrereleaseVersions = true;
        includeAll.includeYankedVersions = true;
        CargoPackageMetadata[] included = operations.FilterCandidates(
            dependency,
            candidates,
            includeAll);

        Assert.That(included, Is.EqualTo(new CargoPackageMetadata[]
        {
            stable,
            prerelease,
            yanked
        }));
        Assert.That(
            operations.SelectHighestCandidate(dependency, candidates, includeAll),
            Is.SameAs(yanked));

        CargoResolverOptions replacement = new CargoResolverOptions();
        CargoRegistrySource mirrorSource = new CargoRegistrySource(
            "mirror",
            "https://example.test/mirror-index");
        replacement.sourceReplacements = new CargoSourceReplacement[]
        {
            new CargoSourceReplacement("crates-io", mirrorSource)
        };

        CargoPackageMetadata[] replaced = operations.FilterCandidates(
            dependency,
            candidates,
            replacement);

        Assert.That(replaced, Is.EqualTo(new CargoPackageMetadata[] { mirror }));
    }

    [Test]
    public void CandidateFeatureOperations_RequireSupportedFeaturesAndSelectHighest()
    {
        ICargoResolution operations = new CargoOperations();
        CargoDependency dependency = Dependency("1");
        CargoPackageMetadata complete = Package(
            operations,
            "demo",
            "1.1.0",
            "crates-io",
            false,
            new string[] { "derive", "std" });
        CargoPackageMetadata incomplete = Package(
            operations,
            "demo",
            "1.2.0",
            "crates-io",
            false,
            new string[] { "std" });
        CargoResolvedDependencyFeatures selectedFeatures = new CargoResolvedDependencyFeatures(
            new CargoPackageName("demo"),
            CargoDependencyKind.Normal,
            true,
            new string[] { "derive", "std" });
        CargoPackageMetadata[] candidates = new CargoPackageMetadata[]
        {
            complete,
            incomplete
        };

        CargoPackageMetadata[] filtered = operations.FilterCandidatesWithFeatures(
            dependency,
            candidates,
            new CargoResolverOptions(),
            selectedFeatures);
        CargoPackageMetadata? selected = operations.SelectHighestCandidateWithFeatures(
            dependency,
            candidates,
            new CargoResolverOptions(),
            selectedFeatures);

        Assert.That(filtered, Is.EqualTo(new CargoPackageMetadata[] { complete }));
        Assert.That(selected, Is.SameAs(complete));
    }

    [Test]
    public void ManifestAndFeatureOperations_ParseFormsAndActivateOptionalDependencies()
    {
        ICargoResolution operations = new CargoOperations();
        CargoManifest manifest = operations.ParseManifest("""
            [dependencies]
            log = "0.4"
            serde = { version = "1", optional = true, default-features = false, features = ["alloc"] }

            [dev-dependencies]
            test-log = "0.2"

            [features]
            default = ["serde-support"]
            serde-support = ["dep:serde", "serde/derive"]

            [registries.mirror]
            index = "https://example.test/mirror-index"
            """);

        CargoFeatureResolution resolution = operations.ResolveFeatures(
            manifest,
            new CargoResolverOptions(),
            new string[0],
            true,
            false);

        Assert.That(manifest.dependencies, Has.Length.EqualTo(3));
        Assert.That(manifest.dependencies[0].packageName.value, Is.EqualTo("log"));
        Assert.That(manifest.dependencies[1].optional, Is.True);
        Assert.That(manifest.dependencies[2].dependencyKind, Is.EqualTo(CargoDependencyKind.Development));
        Assert.That(manifest.features, Has.Length.EqualTo(2));
        Assert.That(manifest.registrySources, Has.Length.EqualTo(1));
        Assert.That(resolution.rootFeatures, Is.EqualTo(new string[]
        {
            "default",
            "serde-support"
        }));
        Assert.That(resolution.activeDependencies, Has.Length.EqualTo(2));
        Assert.That(resolution.dependencyFeatures, Has.Length.EqualTo(2));
        Assert.That(resolution.dependencyFeatures[1].packageName.value, Is.EqualTo("serde"));
        Assert.That(resolution.dependencyFeatures[1].defaultFeatures, Is.False);
        Assert.That(
            resolution.dependencyFeatures[1].features,
            Is.EqualTo(new string[] { "alloc", "derive" }));
    }

    [Test]
    public void DependencyResolution_AppliesExternalYankedVersionsWithoutMutatingMetadata()
    {
        ICargoResolution operations = new CargoOperations();
        CargoPackageMetadata one = Package(
            operations,
            "demo",
            "1.0.0",
            "crates-io",
            false,
            new string[0]);
        CargoPackageMetadata high = Package(
            operations,
            "demo",
            "1.5.0",
            "crates-io",
            false,
            new string[0]);
        CargoYankedVersion yanked = new CargoYankedVersion(
            high.identity,
            "withdrawn release");
        CargoDependency[] roots = new CargoDependency[] { Dependency("1") };
        CargoPackageMetadata[] packages = new CargoPackageMetadata[] { one, high };

        CargoDependencyResolutionResult defaultResult = operations.ResolveDependencies(
            roots,
            packages,
            new CargoYankedVersion[] { yanked },
            new CargoResolverOptions());
        CargoResolverOptions includeYanked = new CargoResolverOptions();
        includeYanked.includeYankedVersions = true;
        CargoDependencyResolutionResult includedResult = operations.ResolveDependencies(
            roots,
            packages,
            new CargoYankedVersion[] { yanked },
            includeYanked);

        Assert.That(defaultResult.conflicts, Is.Empty);
        Assert.That(defaultResult.nodes, Has.Length.EqualTo(1));
        Assert.That(
            defaultResult.nodes[0].packageMetadata.identity.version.originalText,
            Is.EqualTo("1.0.0"));
        Assert.That(includedResult.conflicts, Is.Empty);
        Assert.That(
            includedResult.nodes[0].packageMetadata.identity.version.originalText,
            Is.EqualTo("1.5.0"));
        Assert.That(high.yanked, Is.False);
    }

    [Test]
    public void ManifestDependencyResolution_UsesActivatedRootDependencies()
    {
        ICargoResolution operations = new CargoOperations();
        CargoManifest manifest = operations.ParseManifest("""
            [dependencies]
            app = "1"
            """);
        CargoPackageMetadata app = Package(
            operations,
            "app",
            "1.2.0",
            "crates-io",
            false,
            new string[0]);

        CargoDependencyResolutionResult result = operations.ResolveManifestDependencies(
            manifest,
            new CargoPackageMetadata[] { app },
            new CargoYankedVersion[0],
            new CargoResolverOptions());

        Assert.That(result.conflicts, Is.Empty);
        Assert.That(result.nodes, Has.Length.EqualTo(1));
        Assert.That(result.nodes[0].packageMetadata, Is.SameAs(app));
        Assert.That(result.nodes[0].direct, Is.True);
    }

    static CargoDependency Dependency(string requirement)
    {
        return new CargoDependency(
            new CargoPackageName("demo"),
            requirement,
            CargoDependencyKind.Normal,
            false,
            true,
            new string[0],
            null,
            null);
    }

    static CargoPackageMetadata Package(
        ICargoResolution operations,
        string packageName,
        string versionText,
        string sourceName,
        bool yanked,
        string[] featureNames)
    {
        VersionParseResult parsed = operations.ParseVersion(versionText);
        Assert.That(parsed.success, Is.True, versionText);
        Assert.That(parsed.version, Is.Not.Null, versionText);
        CargoFeature[] features = new CargoFeature[featureNames.Length];
        int index = 0;

        while (index < featureNames.Length)
        {
            features[index] = new CargoFeature(featureNames[index], new string[0]);
            index = index + 1;
        }

        return new CargoPackageMetadata(
            new CargoPackageVersionIdentity(new CargoPackageName(packageName), parsed.version!),
            new CargoDependency[0],
            features,
            new CargoRegistrySource(
                sourceName,
                "https://example.test/" + sourceName + "-index"),
            yanked);
    }
}
