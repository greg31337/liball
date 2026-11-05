using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class CargoMetadataModelTests
{
    [Test]
    public void PackageName_ValidatesAndNormalizesNames()
    {
        CargoPackageName name = new CargoPackageName("Serde_JSON");

        Assert.That(name.GetValue(), Is.EqualTo("Serde_JSON"));
        Assert.That(name.GetNormalizedValue(), Is.EqualTo("serde_json"));
        Assert.That(name.MatchesText("serde_json"), Is.True);
        Assert.That(name.EqualsName(new CargoPackageName("serde_json")), Is.True);

        Assert.That(
            delegate
            {
                new CargoPackageName("");
            },
            Throws.TypeOf<ArgumentException>());
        Assert.That(
            delegate
            {
                new CargoPackageName("-bad");
            },
            Throws.TypeOf<ArgumentException>());
        Assert.That(
            delegate
            {
                new CargoPackageName("bad name");
            },
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void PackageVersionIdentity_RequiresCargoVersionAndComparesIdentities()
    {
        CargoPackageVersionIdentity identity = CargoPackageVersionIdentity.Create("serde", "1.0.0");

        Assert.That(identity.GetIdentityText(), Is.EqualTo("serde 1.0.0"));
        Assert.That(identity.GetPackageName().MatchesText("serde"), Is.True);
        Assert.That(identity.GetVersion().GetDialect(), Is.EqualTo(VersionDialect.Cargo));
        Assert.That(identity.HasSameIdentity(CargoPackageVersionIdentity.Create("SERDE", "1.0.0+build.1")), Is.True);

        Assert.That(
            delegate
            {
                CargoPackageVersionIdentity.Create("serde", "v1.0.0");
            },
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Dependency_StoresRequirementKindFeaturesRegistryAndTarget()
    {
        string[] features = new string[] { "derive", "dep:alloc" };
        CargoDependency dependency = new CargoDependency(
            new CargoPackageName("serde"),
            "^1.0",
            CargoDependencyKind.Build,
            true,
            false,
            features,
            "crates-io",
            "x86_64-unknown-linux-gnu");

        features[0] = "changed";
        string[] returnedFeatures = dependency.GetFeatures();
        returnedFeatures[1] = "changed";

        Assert.That(dependency.GetPackageName().MatchesText("serde"), Is.True);
        Assert.That(dependency.HasVersionRequirement(), Is.True);
        Assert.That(dependency.GetVersionRequirementText(), Is.EqualTo("^1.0"));
        Assert.That(dependency.AllowsVersionText("1.5.0"), Is.True);
        Assert.That(dependency.AllowsVersionText("2.0.0"), Is.False);
        Assert.That(dependency.GetDependencyKind(), Is.EqualTo(CargoDependencyKind.Build));
        Assert.That(dependency.IsOptional(), Is.True);
        Assert.That(dependency.UsesDefaultFeatures(), Is.False);
        Assert.That(dependency.GetFeature(0), Is.EqualTo("derive"));
        Assert.That(dependency.GetFeature(1), Is.EqualTo("dep:alloc"));
        Assert.That(dependency.HasRegistryName(), Is.True);
        Assert.That(dependency.GetRegistryName(), Is.EqualTo("crates-io"));
        Assert.That(dependency.HasTargetTriple(), Is.True);
        Assert.That(dependency.GetTargetTriple(), Is.EqualTo("x86_64-unknown-linux-gnu"));
    }

    [Test]
    public void FeaturePackageMetadataAndYankedVersions_DefensivelyCopyArrays()
    {
        CargoDependency dependency = CargoDependency.Create("serde", "1");
        CargoFeature feature = CargoFeature.Create("default", new string[] { "std" });
        CargoDependency[] dependencies = new CargoDependency[] { dependency };
        CargoFeature[] features = new CargoFeature[] { feature };
        CargoPackageMetadata metadata = new CargoPackageMetadata(
            CargoPackageVersionIdentity.Create("demo", "1.2.3"),
            dependencies,
            features,
            CargoRegistrySource.Create("crates-io", "https://github.com/rust-lang/crates.io-index"),
            true);
        CargoYankedVersion yanked = CargoYankedVersion.Create("demo", "1.2.3", "security issue");

        dependencies[0] = CargoDependency.Create("other", "1");
        features[0] = CargoFeature.Create("other", new string[0]);
        CargoDependency[] returnedDependencies = metadata.GetDependencies();
        CargoFeature[] returnedFeatures = metadata.GetFeatures();
        returnedDependencies[0] = CargoDependency.Create("changed", "1");
        returnedFeatures[0] = CargoFeature.Create("changed", new string[0]);

        Assert.That(metadata.GetIdentity().GetIdentityText(), Is.EqualTo("demo 1.2.3"));
        Assert.That(metadata.GetDependency(0).GetPackageName().MatchesText("serde"), Is.True);
        Assert.That(metadata.GetFeature(0).GetName(), Is.EqualTo("default"));
        Assert.That(metadata.GetFeature(0).GetMember(0), Is.EqualTo("std"));
        Assert.That(metadata.GetRegistrySource().GetName(), Is.EqualTo("crates-io"));
        Assert.That(metadata.IsYanked(), Is.True);
        Assert.That(yanked.GetPackageName().MatchesText("demo"), Is.True);
        Assert.That(yanked.GetVersionText(), Is.EqualTo("1.2.3"));
        Assert.That(yanked.HasReason(), Is.True);
    }

    [Test]
    public void RegistrySourceReplacementAndResolverOptions_ExposeConfiguredMetadata()
    {
        CargoRegistrySource cratesIo = CargoRegistrySource.Create("crates-io", "https://github.com/rust-lang/crates.io-index");
        CargoRegistrySource mirror = CargoRegistrySource.Create("mirror", "https://example.test/index");
        CargoSourceReplacement replacement = CargoSourceReplacement.Create("crates-io", mirror);
        CargoResolverOptions options = CargoResolverOptions.CreateDefault();

        options.SetResolverVersion(CargoResolverVersion.Version3);
        options.SetIncludeYankedVersions(true);
        options.SetIncludePrereleaseVersions(true);
        options.SetTargetTriple("wasm32-unknown-unknown");
        CargoRegistrySource[] registries = new CargoRegistrySource[] { cratesIo };
        CargoSourceReplacement[] replacements = new CargoSourceReplacement[] { replacement };
        options.SetRegistrySources(registries);
        options.SetSourceReplacements(replacements);
        registries[0] = mirror;
        replacements[0] = CargoSourceReplacement.Create("mirror", cratesIo);

        Assert.That(cratesIo.MatchesName("crates-io"), Is.True);
        Assert.That(replacement.ReplacesSource("crates-io"), Is.True);
        Assert.That(replacement.GetReplacementSource().GetName(), Is.EqualTo("mirror"));
        Assert.That(options.GetResolverVersion(), Is.EqualTo(CargoResolverVersion.Version3));
        Assert.That(options.IncludesYankedVersions(), Is.True);
        Assert.That(options.IncludesPrereleaseVersions(), Is.True);
        Assert.That(options.HasTargetTriple(), Is.True);
        Assert.That(options.GetTargetTriple(), Is.EqualTo("wasm32-unknown-unknown"));
        Assert.That(options.GetRegistrySource(0).GetName(), Is.EqualTo("crates-io"));
        Assert.That(options.GetSourceReplacement(0).GetSourceName(), Is.EqualTo("crates-io"));
    }

    [Test]
    public void Constructors_RejectInvalidArraysAndEnumValues()
    {
        Assert.That(
            delegate
            {
                new CargoDependency(
                    new CargoPackageName("serde"),
                    "^1.0",
                    (CargoDependencyKind)99,
                    false,
                    true,
                    new string[0],
                    null,
                    null);
            },
            Throws.TypeOf<ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                CargoFeature.Create("default", new string[] { "" });
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                CargoPackageMetadata.Create(
                    CargoPackageVersionIdentity.Create("demo", "1.0.0"),
                    new CargoDependency[] { null! },
                    new CargoFeature[0],
                    CargoRegistrySource.Create("crates-io", "https://example.test/index"));
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                CargoResolverOptions options = CargoResolverOptions.CreateDefault();
                options.SetResolverVersion((CargoResolverVersion)99);
            },
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
