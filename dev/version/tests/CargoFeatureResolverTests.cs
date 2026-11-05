using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class CargoFeatureResolverTests
{
    [Test]
    public void ResolveFeatures_ActivatesDefaultTransitiveAndOptionalDependencyFeatures()
    {
        CargoManifest manifest = CargoVersions.ParseManifest("""
            [dependencies]
            log = "0.4"
            serde = { version = "1", optional = true, default-features = false, features = ["alloc"] }
            png = { version = "0.17", optional = true }

            [features]
            default = ["image"]
            image = ["dep:serde", "serde/derive", "png"]
            """);
        CargoFeatureResolution resolution = CargoVersions.ResolveFeatures(
            manifest,
            CargoResolverOptions.CreateDefault(),
            new string[0],
            true,
            false);

        Assert.That(resolution.HasRootFeature("default"), Is.True);
        Assert.That(resolution.HasRootFeature("image"), Is.True);
        Assert.That(resolution.HasRootFeature("png"), Is.True);
        Assert.That(resolution.GetActiveDependencyCount(), Is.EqualTo(3));

        CargoResolvedDependencyFeatures? serde = resolution.FindDependencyFeatures("serde");
        Assert.That(serde, Is.Not.Null);
        Assert.That(serde!.UsesDefaultFeatures(), Is.False);
        Assert.That(serde.HasFeature("alloc"), Is.True);
        Assert.That(serde.HasFeature("derive"), Is.True);

        CargoResolvedDependencyFeatures? png = resolution.FindDependencyFeatures("png");
        Assert.That(png, Is.Not.Null);
        Assert.That(png!.UsesDefaultFeatures(), Is.True);
    }

    [Test]
    public void ResolveFeatures_ConditionalDependencyFeatureDoesNotActivateOptionalDependency()
    {
        CargoManifest manifest = CargoVersions.ParseManifest("""
            [dependencies]
            serde = { version = "1", optional = true, default-features = false }

            [features]
            serde-derive = ["serde?/derive"]
            with-serde = ["dep:serde", "serde?/derive"]
            """);

        CargoFeatureResolution conditionalOnly = CargoVersions.ResolveFeatures(
            manifest,
            CargoResolverOptions.CreateDefault(),
            new string[] { "serde-derive" },
            false,
            false);
        CargoFeatureResolution activated = CargoVersions.ResolveFeatures(
            manifest,
            CargoResolverOptions.CreateDefault(),
            new string[] { "with-serde" },
            false,
            false);

        Assert.That(conditionalOnly.GetActiveDependencyCount(), Is.EqualTo(0));
        Assert.That(conditionalOnly.FindDependencyFeatures("serde"), Is.Null);
        Assert.That(activated.GetActiveDependencyCount(), Is.EqualTo(1));
        Assert.That(activated.FindDependencyFeatures("serde")!.HasFeature("derive"), Is.True);
    }

    [Test]
    public void ResolveFeatures_ResolverVersionOneUnifiesBuildAndDevFeatureContributors()
    {
        CargoManifest manifest = CargoVersions.ParseManifest("""
            [dependencies]
            serde = { version = "1", default-features = false, features = ["alloc"] }

            [build-dependencies]
            serde = { version = "1", features = ["std"] }

            [dev-dependencies]
            serde = { version = "1", features = ["derive"] }
            """);
        CargoResolverOptions options = CargoResolverOptions.CreateDefault();
        options.SetResolverVersion(CargoResolverVersion.Version1);

        CargoFeatureResolution resolution = CargoVersions.ResolveFeatures(
            manifest,
            options,
            new string[0],
            false,
            false);

        Assert.That(resolution.GetDependencyFeatureCount(), Is.EqualTo(1));
        CargoResolvedDependencyFeatures serde = resolution.GetDependencyFeatures(0);
        Assert.That(serde.UsesDefaultFeatures(), Is.True);
        Assert.That(serde.HasFeature("alloc"), Is.True);
        Assert.That(serde.HasFeature("std"), Is.True);
        Assert.That(serde.HasFeature("derive"), Is.True);
    }

    [Test]
    public void ResolveFeatures_ResolverVersionTwoKeepsBuildAndDevFeaturesSeparate()
    {
        CargoManifest manifest = CargoVersions.ParseManifest("""
            [dependencies]
            serde = { version = "1", default-features = false, features = ["alloc"] }

            [build-dependencies]
            serde = { version = "1", features = ["std"] }

            [dev-dependencies]
            serde = { version = "1", features = ["derive"] }
            """);
        CargoResolverOptions options = CargoResolverOptions.CreateDefault();
        options.SetResolverVersion(CargoResolverVersion.Version2);

        CargoFeatureResolution normalBuild = CargoVersions.ResolveFeatures(
            manifest,
            options,
            new string[0],
            false,
            false);
        CargoFeatureResolution withDev = CargoVersions.ResolveFeatures(
            manifest,
            options,
            new string[0],
            false,
            true);

        Assert.That(normalBuild.GetDependencyFeatureCount(), Is.EqualTo(2));
        Assert.That(normalBuild.GetDependencyFeatures(0).GetDependencyKind(), Is.EqualTo(CargoDependencyKind.Normal));
        Assert.That(normalBuild.GetDependencyFeatures(0).HasFeature("alloc"), Is.True);
        Assert.That(normalBuild.GetDependencyFeatures(0).HasFeature("std"), Is.False);
        Assert.That(normalBuild.GetDependencyFeatures(1).GetDependencyKind(), Is.EqualTo(CargoDependencyKind.Build));
        Assert.That(normalBuild.GetDependencyFeatures(1).HasFeature("std"), Is.True);

        Assert.That(withDev.GetDependencyFeatureCount(), Is.EqualTo(3));
        Assert.That(withDev.GetDependencyFeatures(2).GetDependencyKind(), Is.EqualTo(CargoDependencyKind.Development));
        Assert.That(withDev.GetDependencyFeatures(2).HasFeature("derive"), Is.True);
    }

    [Test]
    public void ResolveFeatures_ResolverVersionTwoFiltersInactiveTargetDependencies()
    {
        CargoManifest manifest = CargoVersions.ParseManifest("""
            [dependencies]
            common = { version = "1", default-features = false, features = ["base"] }

            [target.'cfg(windows)'.dependencies]
            common = { version = "1", features = ["win"] }

            [target.'cfg(unix)'.dependencies]
            common = { version = "1", features = ["unix"] }
            """);
        CargoResolverOptions options = CargoResolverOptions.CreateDefault();
        options.SetResolverVersion(CargoResolverVersion.Version2);
        options.SetTargetTriple("x86_64-unknown-linux-gnu");

        CargoFeatureResolution resolution = CargoVersions.ResolveFeatures(
            manifest,
            options,
            new string[0],
            false,
            false);

        Assert.That(resolution.GetActiveDependencyCount(), Is.EqualTo(2));
        Assert.That(resolution.GetDependencyFeatureCount(), Is.EqualTo(1));
        CargoResolvedDependencyFeatures common = resolution.GetDependencyFeatures(0);
        Assert.That(common.HasFeature("base"), Is.True);
        Assert.That(common.HasFeature("unix"), Is.True);
        Assert.That(common.HasFeature("win"), Is.False);
    }

    [Test]
    public void ResolveFeatures_ResolverVersionOneUnifiesInactiveTargetDependencyFeatures()
    {
        CargoManifest manifest = CargoVersions.ParseManifest("""
            [dependencies]
            common = { version = "1", default-features = false, features = ["base"] }

            [target.'cfg(windows)'.dependencies]
            common = { version = "1", features = ["win"] }
            """);
        CargoResolverOptions options = CargoResolverOptions.CreateDefault();
        options.SetResolverVersion(CargoResolverVersion.Version1);
        options.SetTargetTriple("x86_64-unknown-linux-gnu");

        CargoFeatureResolution resolution = CargoVersions.ResolveFeatures(
            manifest,
            options,
            new string[0],
            false,
            false);

        Assert.That(resolution.GetActiveDependencyCount(), Is.EqualTo(2));
        Assert.That(resolution.GetDependencyFeatureCount(), Is.EqualTo(1));
        Assert.That(resolution.GetDependencyFeatures(0).HasFeature("base"), Is.True);
        Assert.That(resolution.GetDependencyFeatures(0).HasFeature("win"), Is.True);
    }
}
