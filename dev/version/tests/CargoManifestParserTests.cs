using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class CargoManifestParserTests
{
    [Test]
    public void ParseManifest_ReadsDependencySectionsAndInlineOptions()
    {
        string manifest = """
            [dependencies]
            serde = "1"
            regex = { version = "^1.7", optional = true, default-features = false, features = ["std", "perf"], registry = "mirror" }

            [dev-dependencies]
            pretty_assertions = "1"

            [build-dependencies]
            cc = "1"

            [target.'cfg(unix)'.dependencies]
            libc = "0.2"
            """;

        CargoManifest parsed = CargoVersions.ParseManifest(manifest);

        Assert.That(parsed.GetDependencyCount(), Is.EqualTo(5));
        AssertDependency(parsed.GetDependency(0), "serde", "1", CargoDependencyKind.Normal);
        AssertDependency(parsed.GetDependency(2), "pretty_assertions", "1", CargoDependencyKind.Development);
        AssertDependency(parsed.GetDependency(3), "cc", "1", CargoDependencyKind.Build);

        CargoDependency regex = parsed.GetDependency(1);
        Assert.That(regex.GetPackageName().MatchesText("regex"), Is.True);
        Assert.That(regex.GetVersionRequirementText(), Is.EqualTo("^1.7"));
        Assert.That(regex.IsOptional(), Is.True);
        Assert.That(regex.UsesDefaultFeatures(), Is.False);
        Assert.That(regex.GetFeatureCount(), Is.EqualTo(2));
        Assert.That(regex.GetFeature(0), Is.EqualTo("std"));
        Assert.That(regex.GetFeature(1), Is.EqualTo("perf"));
        Assert.That(regex.HasRegistryName(), Is.True);
        Assert.That(regex.GetRegistryName(), Is.EqualTo("mirror"));

        CargoDependency libc = parsed.GetDependency(4);
        Assert.That(libc.GetPackageName().MatchesText("libc"), Is.True);
        Assert.That(libc.HasTargetTriple(), Is.True);
        Assert.That(libc.GetTargetTriple(), Is.EqualTo("cfg(unix)"));
    }

    [Test]
    public void ParseManifest_ReadsFeaturesRegistriesAndSourceReplacements()
    {
        string manifest = """
            [features]
            default = ["std", "dep:serde"]
            std = ["serde/std"]

            [source.crates-io]
            replace-with = "mirror"

            [source.mirror]
            registry = "https://example.test/mirror-index"
            """;

        CargoManifest parsed = CargoVersions.ParseManifest(manifest);

        Assert.That(parsed.GetFeatureCount(), Is.EqualTo(2));
        Assert.That(parsed.GetFeature(0).GetName(), Is.EqualTo("default"));
        Assert.That(parsed.GetFeature(0).GetMember(0), Is.EqualTo("std"));
        Assert.That(parsed.GetFeature(0).GetMember(1), Is.EqualTo("dep:serde"));
        Assert.That(parsed.GetFeature(1).GetName(), Is.EqualTo("std"));
        Assert.That(parsed.GetFeature(1).GetMember(0), Is.EqualTo("serde/std"));

        Assert.That(parsed.GetRegistrySourceCount(), Is.EqualTo(1));
        Assert.That(parsed.GetRegistrySource(0).GetName(), Is.EqualTo("mirror"));
        Assert.That(parsed.GetRegistrySource(0).GetIndexUrl(), Is.EqualTo("https://example.test/mirror-index"));

        Assert.That(parsed.GetSourceReplacementCount(), Is.EqualTo(1));
        Assert.That(parsed.GetSourceReplacement(0).GetSourceName(), Is.EqualTo("crates-io"));
        Assert.That(parsed.GetSourceReplacement(0).GetReplacementSource().GetName(), Is.EqualTo("mirror"));
    }

    [Test]
    public void ParseManifest_IgnoresCommentsAndUnknownSections()
    {
        string manifest = """
            # top comment
            [package]
            name = "ignored"

            [dependencies]
            anyhow = "1" # trailing comment

            [registries.mirror]
            index = "https://example.test/index#fragment"
            """;

        CargoManifest parsed = CargoVersions.ParseManifest(manifest);

        Assert.That(parsed.GetDependencyCount(), Is.EqualTo(1));
        Assert.That(parsed.GetDependency(0).GetPackageName().MatchesText("anyhow"), Is.True);
        Assert.That(parsed.GetRegistrySourceCount(), Is.EqualTo(1));
        Assert.That(parsed.GetRegistrySource(0).GetIndexUrl(), Is.EqualTo("https://example.test/index#fragment"));
    }

    [Test]
    public void ParseManifest_RejectsMalformedDependencyAndUnresolvedSourceReplacement()
    {
        Assert.That(
            delegate
            {
                CargoVersions.ParseManifest("""
                    [dependencies]
                    serde = { version = "not a requirement" }
                    """);
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                CargoVersions.ParseManifest("""
                    [source.crates-io]
                    replace-with = "missing"
                    """);
            },
            Throws.TypeOf<ArgumentException>());
    }

    static void AssertDependency(
        CargoDependency dependency,
        string packageName,
        string versionRequirement,
        CargoDependencyKind dependencyKind)
    {
        Assert.That(dependency.GetPackageName().MatchesText(packageName), Is.True);
        Assert.That(dependency.GetVersionRequirementText(), Is.EqualTo(versionRequirement));
        Assert.That(dependency.GetDependencyKind(), Is.EqualTo(dependencyKind));
        Assert.That(dependency.IsOptional(), Is.False);
        Assert.That(dependency.UsesDefaultFeatures(), Is.True);
    }
}
