using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class CargoOperationsReferenceTests
{
    [TestCase("1.2.3", "1.2.3", true)]
    [TestCase("1.2.3", "1.9.9", true)]
    [TestCase("1.2.3", "2.0.0", false)]
    [TestCase("^1.2.3", "1.9.9", true)]
    [TestCase("0.2.3", "0.2.99", true)]
    [TestCase("0.2.3", "0.3.0", false)]
    [TestCase("0.0.3", "0.0.3", true)]
    [TestCase("0.0.3", "0.0.4", false)]
    [TestCase("0.0", "0.0.99", true)]
    [TestCase("0.0", "0.1.0", false)]
    [TestCase("0", "0.9.9", true)]
    [TestCase("0", "1.0.0", false)]
    [TestCase("~1.2.3", "1.2.99", true)]
    [TestCase("~1.2.3", "1.3.0", false)]
    [TestCase("~1", "1.9.9", true)]
    [TestCase("1.*", "1.9.9", true)]
    [TestCase("1.*", "2.0.0", false)]
    [TestCase("1.2.*", "1.2.99", true)]
    [TestCase("1.2.*", "1.3.0", false)]
    [TestCase("*", "999.0.0", true)]
    [TestCase(">=1.2.0", "2.0.0", true)]
    [TestCase(">1", "1.9.9", false)]
    [TestCase(">1", "2.0.0", true)]
    [TestCase("<2", "1.9.9", true)]
    [TestCase("<2", "2.0.0", false)]
    [TestCase("=1.2.3", "1.2.3+build.7", true)]
    [TestCase(">=1.2, <1.5", "1.4.99", true)]
    [TestCase(">=1.2, <1.5", "1.5.0", false)]
    [TestCase("<1.2, ^1.2.2", "1.2.2", false)]
    public void RequirementReference_MatchesDocumentedCandidateBounds(
        string requirement,
        string version,
        bool expected)
    {
        ICargoResolution operations = new CargoOperations();

        VersionRangeParseResult parsed = operations.ParseRequirement(requirement);
        CargoDependency dependency = Dependency("demo", requirement);
        CargoPackageMetadata candidate = Package(
            operations,
            "demo",
            version,
            new CargoDependency[0],
            new CargoFeature[0],
            "crates-io",
            false);
        CargoPackageMetadata[] filtered = operations.FilterCandidates(
            dependency,
            new CargoPackageMetadata[] { candidate },
            new CargoResolverOptions());

        Assert.That(parsed.success, Is.True, requirement);
        Assert.That(parsed.syntax, Is.EqualTo(RangeSyntax.Cargo), requirement);
        Assert.That(filtered.Length == 1, Is.EqualTo(expected), requirement + " with " + version);
    }

    [TestCase("!=1.2.3", CargoRangeDiagnosticCodes.InvalidOperator)]
    [TestCase("1.*.*", CargoRangeDiagnosticCodes.InvalidVersion)]
    [TestCase("1.2.*-alpha", CargoRangeDiagnosticCodes.InvalidVersion)]
    [TestCase("~1.*", CargoRangeDiagnosticCodes.InvalidVersion)]
    [TestCase(">=1.*", CargoRangeDiagnosticCodes.InvalidVersion)]
    [TestCase("1.2.3 || 2.0.0", CargoRangeDiagnosticCodes.InvalidOperator)]
    public void RequirementReference_ReturnsDiagnosticsForMalformedRequirements(
        string requirement,
        string diagnosticCode)
    {
        ICargoResolution operations = new CargoOperations();

        VersionRangeParseResult result = operations.ParseRequirement(requirement);

        Assert.That(result.success, Is.False, requirement);
        Assert.That(result.range, Is.Null, requirement);
        Assert.That(result.syntax, Is.EqualTo(RangeSyntax.Cargo), requirement);
        Assert.That(result.diagnostics, Has.Length.EqualTo(1), requirement);
        Assert.That(result.diagnostics[0].code, Is.EqualTo(diagnosticCode), requirement);
    }

    [Test]
    public void PrereleaseReference_AllowsOnlyExplicitSameCoreAndCompatibleUpgrades()
    {
        ICargoResolution operations = new CargoOperations();
        CargoPackageMetadata alpha = SimplePackage(operations, "demo", "1.0.0-alpha.1");
        CargoPackageMetadata beta = SimplePackage(operations, "demo", "1.0.0-beta");
        CargoPackageMetadata release = SimplePackage(operations, "demo", "1.0.0");
        CargoPackageMetadata later = SimplePackage(operations, "demo", "1.2.0");
        CargoPackageMetadata differentCore = SimplePackage(operations, "demo", "1.0.1-alpha");
        CargoPackageMetadata[] candidates = new CargoPackageMetadata[]
        {
            alpha,
            beta,
            release,
            later,
            differentCore
        };

        CargoPackageMetadata[] ordinary = operations.FilterCandidates(
            Dependency("demo", "1.0"),
            candidates,
            new CargoResolverOptions());
        CargoPackageMetadata[] explicitPrerelease = operations.FilterCandidates(
            Dependency("demo", "1.0.0-alpha"),
            candidates,
            new CargoResolverOptions());
        CargoResolverOptions includePrerelease = new CargoResolverOptions();
        includePrerelease.includePrereleaseVersions = true;
        CargoPackageMetadata[] overridden = operations.FilterCandidates(
            Dependency("demo", "1.0"),
            candidates,
            includePrerelease);

        Assert.That(ordinary, Is.EqualTo(new CargoPackageMetadata[] { release, later }));
        Assert.That(explicitPrerelease, Is.EqualTo(new CargoPackageMetadata[]
        {
            alpha,
            beta,
            release,
            later
        }));
        Assert.That(overridden, Is.EqualTo(new CargoPackageMetadata[]
        {
            release,
            later,
            differentCore
        }));
        Assert.That(
            operations.SelectHighestCandidate(
                Dependency("demo", "1.0.0-alpha"),
                candidates,
                new CargoResolverOptions()),
            Is.SameAs(later));
    }

    [Test]
    public void BuildMetadataReference_IsRetainedButIgnoredForPrecedenceAndRequirements()
    {
        ICargoResolution operations = new CargoOperations();

        VersionParseResult parsed = operations.ParseVersion("1.2.3+21AF26D3");
        CargoPackageMetadata plain = SimplePackage(operations, "demo", "1.2.3");
        CargoPackageMetadata built = SimplePackage(operations, "demo", "1.2.3+different");
        CargoPackageMetadata[] exact = operations.FilterCandidates(
            Dependency("demo", "=1.2.3+ignored"),
            new CargoPackageMetadata[] { plain, built },
            new CargoResolverOptions());

        Assert.That(parsed.success, Is.True);
        Assert.That(parsed.version, Is.Not.Null);
        Assert.That(parsed.version!.buildIdentifiers, Has.Length.EqualTo(1));
        Assert.That(parsed.version.buildIdentifiers[0].text, Is.EqualTo("21AF26D3"));
        Assert.That(
            operations.CompareVersions("1.2.3+build.1", "1.2.3+build.2"),
            Is.Zero);
        Assert.That(exact, Is.EqualTo(new CargoPackageMetadata[] { plain, built }));
    }

    [Test]
    public void ManifestReference_ParsesDependencyFormsFeaturesTargetsAndRegistries()
    {
        ICargoResolution operations = new CargoOperations();

        CargoManifest manifest = operations.ParseManifest("""
            [dependencies]
            serde = "1"
            regex = { version = "^1.7", optional = true, default-features = false, features = ["std", "perf"], registry = "mirror" }

            [dev-dependencies]
            pretty_assertions = "1"

            [build-dependencies]
            cc = "1"

            [target.'cfg(unix)'.dependencies]
            libc = "0.2"

            [features]
            default = ["dep:regex"]
            full = ["regex/unicode"]

            [source.crates-io]
            replace-with = "mirror"

            [source.mirror]
            registry = "https://example.test/mirror-index"
            """);

        Assert.That(manifest.dependencies, Has.Length.EqualTo(5));
        AssertDependency(manifest.dependencies[0], "serde", "1", CargoDependencyKind.Normal);
        AssertDependency(manifest.dependencies[2], "pretty_assertions", "1", CargoDependencyKind.Development);
        AssertDependency(manifest.dependencies[3], "cc", "1", CargoDependencyKind.Build);
        Assert.That(manifest.dependencies[1].packageName.value, Is.EqualTo("regex"));
        Assert.That(manifest.dependencies[1].optional, Is.True);
        Assert.That(manifest.dependencies[1].defaultFeatures, Is.False);
        Assert.That(manifest.dependencies[1].features, Is.EqualTo(new string[] { "std", "perf" }));
        Assert.That(manifest.dependencies[1].registryName, Is.EqualTo("mirror"));
        Assert.That(manifest.dependencies[4].targetTriple, Is.EqualTo("cfg(unix)"));
        Assert.That(manifest.features, Has.Length.EqualTo(2));
        Assert.That(manifest.features[0].members, Is.EqualTo(new string[] { "dep:regex" }));
        Assert.That(manifest.registrySources, Has.Length.EqualTo(1));
        Assert.That(manifest.registrySources[0].name, Is.EqualTo("mirror"));
        Assert.That(manifest.sourceReplacements, Has.Length.EqualTo(1));
        Assert.That(manifest.sourceReplacements[0].sourceName, Is.EqualTo("crates-io"));
        Assert.That(manifest.sourceReplacements[0].replacementSource.name, Is.EqualTo("mirror"));
    }

    [Test]
    public void FeatureReference_ActivatesDefaultsOptionalDependenciesAndConditionalFeatures()
    {
        ICargoResolution operations = new CargoOperations();
        CargoManifest manifest = operations.ParseManifest("""
            [dependencies]
            log = "0.4"
            serde = { version = "1", optional = true, default-features = false, features = ["alloc"] }

            [features]
            default = ["serde-support"]
            serde-derive = ["serde?/derive"]
            serde-support = ["dep:serde", "serde?/derive"]
            """);

        CargoFeatureResolution conditionalOnly = operations.ResolveFeatures(
            manifest,
            new CargoResolverOptions(),
            new string[] { "serde-derive" },
            false,
            false);
        CargoFeatureResolution defaults = operations.ResolveFeatures(
            manifest,
            new CargoResolverOptions(),
            new string[0],
            true,
            false);

        Assert.That(conditionalOnly.activeDependencies, Has.Length.EqualTo(1));
        Assert.That(FindDependencyFeatures(conditionalOnly, "serde"), Is.Null);
        Assert.That(defaults.rootFeatures, Is.EqualTo(new string[]
        {
            "default",
            "serde-support"
        }));
        Assert.That(defaults.activeDependencies, Has.Length.EqualTo(2));
        CargoResolvedDependencyFeatures? serde = FindDependencyFeatures(defaults, "serde");
        Assert.That(serde, Is.Not.Null);
        Assert.That(serde!.defaultFeatures, Is.False);
        Assert.That(serde.features, Is.EqualTo(new string[] { "alloc", "derive" }));
    }

    [Test]
    public void ResolverModeReference_SeparatesDependencyKindsAfterVersionOne()
    {
        ICargoResolution operations = new CargoOperations();
        CargoManifest manifest = operations.ParseManifest("""
            [dependencies]
            serde = { version = "1", default-features = false, features = ["alloc"] }

            [build-dependencies]
            serde = { version = "1", features = ["std"] }

            [dev-dependencies]
            serde = { version = "1", features = ["derive"] }
            """);
        CargoResolverOptions versionOne = new CargoResolverOptions();
        versionOne.resolverVersion = CargoResolverVersion.Version1;
        CargoResolverOptions versionTwo = new CargoResolverOptions();
        versionTwo.resolverVersion = CargoResolverVersion.Version2;
        CargoResolverOptions versionThree = new CargoResolverOptions();
        versionThree.resolverVersion = CargoResolverVersion.Version3;

        CargoFeatureResolution one = operations.ResolveFeatures(
            manifest,
            versionOne,
            new string[0],
            false,
            false);
        CargoFeatureResolution two = operations.ResolveFeatures(
            manifest,
            versionTwo,
            new string[0],
            false,
            true);
        CargoFeatureResolution three = operations.ResolveFeatures(
            manifest,
            versionThree,
            new string[0],
            false,
            true);

        Assert.That(one.dependencyFeatures, Has.Length.EqualTo(1));
        Assert.That(one.dependencyFeatures[0].defaultFeatures, Is.True);
        Assert.That(one.dependencyFeatures[0].features, Is.EqualTo(new string[]
        {
            "alloc",
            "std",
            "derive"
        }));
        Assert.That(two.dependencyFeatures, Has.Length.EqualTo(3));
        Assert.That(two.dependencyFeatures[0].dependencyKind, Is.EqualTo(CargoDependencyKind.Normal));
        Assert.That(two.dependencyFeatures[1].dependencyKind, Is.EqualTo(CargoDependencyKind.Build));
        Assert.That(two.dependencyFeatures[2].dependencyKind, Is.EqualTo(CargoDependencyKind.Development));
        Assert.That(three.dependencyFeatures, Has.Length.EqualTo(3));
    }

    [Test]
    public void TargetReference_FiltersInactiveTargetDependenciesForModernResolvers()
    {
        ICargoResolution operations = new CargoOperations();
        CargoManifest manifest = operations.ParseManifest("""
            [dependencies]
            common = "1"

            [target.'cfg(windows)'.dependencies]
            winapi = "1"

            [target.'cfg(unix)'.dependencies]
            libc = "1"

            [target.'cfg(target_arch = "x86_64")'.dependencies]
            arch = "1"
            """);
        CargoResolverOptions linux = new CargoResolverOptions();
        linux.resolverVersion = CargoResolverVersion.Version2;
        linux.targetTriple = "x86_64-unknown-linux-gnu";
        linux.targetTripleSpecified = true;

        CargoFeatureResolution resolution = operations.ResolveFeatures(
            manifest,
            linux,
            new string[0],
            false,
            false);

        Assert.That(DependencyNames(resolution), Is.EqualTo(new string[]
        {
            "common",
            "libc",
            "arch"
        }));
    }

    [Test]
    public void RegistryReference_AppliesNamedRegistriesAndSourceReplacement()
    {
        ICargoResolution operations = new CargoOperations();
        CargoPackageMetadata crates = Package(
            operations,
            "demo",
            "1.0.0",
            new CargoDependency[0],
            new CargoFeature[0],
            "crates-io",
            false);
        CargoPackageMetadata mirror = Package(
            operations,
            "demo",
            "1.1.0",
            new CargoDependency[0],
            new CargoFeature[0],
            "mirror",
            false);
        CargoPackageMetadata privatePackage = Package(
            operations,
            "private-lib",
            "1.2.0",
            new CargoDependency[0],
            new CargoFeature[0],
            "private",
            false);
        CargoRegistrySource mirrorSource = new CargoRegistrySource(
            "mirror",
            "https://example.test/mirror-index");
        CargoResolverOptions replacement = new CargoResolverOptions();
        replacement.sourceReplacements = new CargoSourceReplacement[]
        {
            new CargoSourceReplacement("crates-io", mirrorSource)
        };
        CargoResolverOptions privateOptions = new CargoResolverOptions();
        privateOptions.registrySources = new CargoRegistrySource[]
        {
            new CargoRegistrySource("private", "https://example.test/private-index")
        };
        CargoDependency privateDependency = new CargoDependency(
            new CargoPackageName("private-lib"),
            "1",
            CargoDependencyKind.Normal,
            false,
            true,
            new string[0],
            "private",
            null);

        CargoPackageMetadata[] replaced = operations.FilterCandidates(
            Dependency("demo", "1"),
            new CargoPackageMetadata[] { crates, mirror },
            replacement);
        CargoPackageMetadata[] fromPrivate = operations.FilterCandidates(
            privateDependency,
            new CargoPackageMetadata[] { privatePackage },
            privateOptions);

        Assert.That(replaced, Is.EqualTo(new CargoPackageMetadata[] { mirror }));
        Assert.That(fromPrivate, Is.EqualTo(new CargoPackageMetadata[] { privatePackage }));
    }

    [Test]
    public void CandidateReference_SkipsYankedAndMissingFeatureVersionsThenSelectsHighest()
    {
        ICargoResolution operations = new CargoOperations();
        CargoPackageMetadata supported = PackageWithFeatureNames(
            operations,
            "demo",
            "1.0.0",
            false,
            new string[] { "derive" });
        CargoPackageMetadata missingFeature = PackageWithFeatureNames(
            operations,
            "demo",
            "1.2.0",
            false,
            new string[] { "std" });
        CargoPackageMetadata yanked = PackageWithFeatureNames(
            operations,
            "demo",
            "1.1.0",
            true,
            new string[] { "derive" });
        CargoResolvedDependencyFeatures features = new CargoResolvedDependencyFeatures(
            new CargoPackageName("demo"),
            CargoDependencyKind.Normal,
            true,
            new string[] { "derive" });
        CargoPackageMetadata[] candidates = new CargoPackageMetadata[]
        {
            supported,
            missingFeature,
            yanked
        };

        CargoPackageMetadata[] filtered = operations.FilterCandidatesWithFeatures(
            Dependency("demo", "1"),
            candidates,
            new CargoResolverOptions(),
            features);
        CargoPackageMetadata? selected = operations.SelectHighestCandidateWithFeatures(
            Dependency("demo", "1"),
            candidates,
            new CargoResolverOptions(),
            features);

        Assert.That(filtered, Is.EqualTo(new CargoPackageMetadata[] { supported }));
        Assert.That(selected, Is.SameAs(supported));
        Assert.That(candidates[0], Is.SameAs(supported));
    }

    [Test]
    public void YankedReference_ExternalRecordsAreExcludedUnlessExplicitlyIncluded()
    {
        ICargoResolution operations = new CargoOperations();
        CargoPackageMetadata low = SimplePackage(operations, "demo", "1.0.0");
        CargoPackageMetadata high = SimplePackage(operations, "demo", "1.5.0");
        CargoYankedVersion yanked = new CargoYankedVersion(high.identity, "broken release");
        CargoDependency[] roots = new CargoDependency[] { Dependency("demo", "1") };
        CargoPackageMetadata[] packages = new CargoPackageMetadata[] { low, high };

        CargoDependencyResolutionResult excluded = operations.ResolveDependencies(
            roots,
            packages,
            new CargoYankedVersion[] { yanked },
            new CargoResolverOptions());
        CargoResolverOptions include = new CargoResolverOptions();
        include.includeYankedVersions = true;
        CargoDependencyResolutionResult included = operations.ResolveDependencies(
            roots,
            packages,
            new CargoYankedVersion[] { yanked },
            include);

        AssertSelected(excluded, "demo", "1.0.0");
        AssertSelected(included, "demo", "1.5.0");
        Assert.That(high.yanked, Is.False);
    }

    [Test]
    public void ResolverReference_BacktracksFromHighestUnsatisfiedTransitiveVersion()
    {
        ICargoResolution operations = new CargoOperations();
        CargoPackageMetadata appOne = Package(
            operations,
            "app",
            "1.0.0",
            new CargoDependency[] { Dependency("helper", "=1.0.0") },
            new CargoFeature[0],
            "crates-io",
            false);
        CargoPackageMetadata appTwo = Package(
            operations,
            "app",
            "2.0.0",
            new CargoDependency[] { Dependency("helper", "=2.0.0") },
            new CargoFeature[0],
            "crates-io",
            false);
        CargoPackageMetadata helper = SimplePackage(operations, "helper", "1.0.0");

        CargoDependencyResolutionResult result = operations.ResolveDependencies(
            new CargoDependency[] { Dependency("app", ">=1.0.0, <3.0.0") },
            new CargoPackageMetadata[] { appOne, appTwo, helper },
            new CargoYankedVersion[0],
            new CargoResolverOptions());

        Assert.That(result.conflicts, Is.Empty);
        AssertSelected(result, "app", "1.0.0");
        AssertSelected(result, "helper", "1.0.0");
        Assert.That(result.edges, Has.Length.EqualTo(2));
    }

    [Test]
    public void ResolverReference_ReportsConstraintMissingAndCycleDiagnostics()
    {
        ICargoResolution operations = new CargoOperations();
        CargoPackageMetadata a = Package(
            operations,
            "a",
            "1.0.0",
            new CargoDependency[] { Dependency("c", "=1.0.0") },
            new CargoFeature[0],
            "crates-io",
            false);
        CargoPackageMetadata b = Package(
            operations,
            "b",
            "1.0.0",
            new CargoDependency[] { Dependency("c", "=2.0.0") },
            new CargoFeature[0],
            "crates-io",
            false);
        CargoPackageMetadata cOne = SimplePackage(operations, "c", "1.0.0");
        CargoPackageMetadata cTwo = SimplePackage(operations, "c", "2.0.0");
        CargoDependencyResolutionResult constraints = operations.ResolveDependencies(
            new CargoDependency[] { Dependency("a", "1"), Dependency("b", "1") },
            new CargoPackageMetadata[] { a, b, cOne, cTwo },
            new CargoYankedVersion[0],
            new CargoResolverOptions());
        CargoDependencyResolutionResult missing = operations.ResolveDependencies(
            new CargoDependency[] { Dependency("missing", "1") },
            new CargoPackageMetadata[0],
            new CargoYankedVersion[0],
            new CargoResolverOptions());
        CargoPackageMetadata cycleA = Package(
            operations,
            "cycle-a",
            "1.0.0",
            new CargoDependency[] { Dependency("cycle-b", "1") },
            new CargoFeature[0],
            "crates-io",
            false);
        CargoPackageMetadata cycleB = Package(
            operations,
            "cycle-b",
            "1.0.0",
            new CargoDependency[] { Dependency("cycle-a", "1") },
            new CargoFeature[0],
            "crates-io",
            false);
        CargoDependencyResolutionResult cycle = operations.ResolveDependencies(
            new CargoDependency[] { Dependency("cycle-a", "1") },
            new CargoPackageMetadata[] { cycleA, cycleB },
            new CargoYankedVersion[0],
            new CargoResolverOptions());

        AssertConflict(constraints, CargoResolverDiagnosticCodes.UnsatisfiedConstraints);
        AssertConflict(missing, CargoResolverDiagnosticCodes.MissingPackageMetadata);
        AssertConflict(cycle, CargoResolverDiagnosticCodes.CyclicDependency);
    }

    static CargoDependency Dependency(string packageName, string requirement)
    {
        return new CargoDependency(
            new CargoPackageName(packageName),
            requirement,
            CargoDependencyKind.Normal,
            false,
            true,
            new string[0],
            null,
            null);
    }

    static CargoPackageMetadata SimplePackage(
        ICargoResolution operations,
        string packageName,
        string versionText)
    {
        return Package(
            operations,
            packageName,
            versionText,
            new CargoDependency[0],
            new CargoFeature[0],
            "crates-io",
            false);
    }

    static CargoPackageMetadata PackageWithFeatureNames(
        ICargoResolution operations,
        string packageName,
        string versionText,
        bool yanked,
        string[] featureNames)
    {
        CargoFeature[] features = new CargoFeature[featureNames.Length];
        int index = 0;

        while (index < featureNames.Length)
        {
            features[index] = new CargoFeature(featureNames[index], new string[0]);
            index = index + 1;
        }

        return Package(
            operations,
            packageName,
            versionText,
            new CargoDependency[0],
            features,
            "crates-io",
            yanked);
    }

    static CargoPackageMetadata Package(
        ICargoResolution operations,
        string packageName,
        string versionText,
        CargoDependency[] dependencies,
        CargoFeature[] features,
        string sourceName,
        bool yanked)
    {
        VersionParseResult parsed = operations.ParseVersion(versionText);
        Assert.That(parsed.success, Is.True, versionText);
        Assert.That(parsed.version, Is.Not.Null, versionText);

        return new CargoPackageMetadata(
            new CargoPackageVersionIdentity(new CargoPackageName(packageName), parsed.version!),
            dependencies,
            features,
            new CargoRegistrySource(
                sourceName,
                "https://example.test/" + sourceName + "-index"),
            yanked);
    }

    static CargoResolvedDependencyFeatures? FindDependencyFeatures(
        CargoFeatureResolution resolution,
        string packageName)
    {
        int index = 0;

        while (index < resolution.dependencyFeatures.Length)
        {
            if (resolution.dependencyFeatures[index].packageName.normalizedValue
                == packageName.ToLowerInvariant())
            {
                return resolution.dependencyFeatures[index];
            }

            index = index + 1;
        }

        return null;
    }

    static string[] DependencyNames(CargoFeatureResolution resolution)
    {
        string[] names = new string[resolution.activeDependencies.Length];
        int index = 0;

        while (index < resolution.activeDependencies.Length)
        {
            names[index] = resolution.activeDependencies[index].packageName.value;
            index = index + 1;
        }

        return names;
    }

    static void AssertDependency(
        CargoDependency dependency,
        string packageName,
        string requirement,
        CargoDependencyKind kind)
    {
        Assert.That(dependency.packageName.value, Is.EqualTo(packageName));
        Assert.That(dependency.versionRequirementText, Is.EqualTo(requirement));
        Assert.That(dependency.dependencyKind, Is.EqualTo(kind));
    }

    static void AssertSelected(
        CargoDependencyResolutionResult result,
        string packageName,
        string versionText)
    {
        int index = 0;

        while (index < result.nodes.Length)
        {
            CargoPackageVersionIdentity identity = result.nodes[index].packageMetadata.identity;
            if (identity.packageName.normalizedValue == packageName.ToLowerInvariant())
            {
                Assert.That(identity.version.originalText, Is.EqualTo(versionText));
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Expected selected Cargo package " + packageName + ".");
    }

    static void AssertConflict(CargoDependencyResolutionResult result, string diagnosticCode)
    {
        Assert.That(result.conflicts, Has.Length.EqualTo(1));
        Assert.That(result.conflicts[0].diagnosticCode, Is.EqualTo(diagnosticCode));
        Assert.That(result.conflicts[0].message, Is.Not.Empty);
    }
}
