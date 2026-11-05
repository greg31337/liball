using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class CargoCandidateFilterTests
{
    [Test]
    public void FilterCandidates_RejectsYankedPrereleaseAndOutOfRangeVersionsByDefault()
    {
        CargoDependency dependency = CargoDependency.Create("demo", ">=1.0.0, <2.0.0");
        CargoPackageMetadata[] candidates = new CargoPackageMetadata[]
        {
            Metadata("demo", "1.0.0", "crates-io", false, new string[0]),
            Metadata("demo", "1.1.0-alpha.1", "crates-io", false, new string[0]),
            Metadata("demo", "1.2.0", "crates-io", true, new string[0]),
            Metadata("demo", "2.0.0", "crates-io", false, new string[0])
        };

        CargoPackageMetadata[] filtered = CargoVersions.FilterCandidates(
            dependency,
            candidates,
            CargoResolverOptions.CreateDefault());

        Assert.That(filtered.Length, Is.EqualTo(1));
        Assert.That(filtered[0].GetIdentity().GetVersionText(), Is.EqualTo("1.0.0"));
    }

    [Test]
    public void FilterCandidates_IncludeOptionsAllowYankedAndPrereleaseVersions()
    {
        CargoDependency dependency = CargoDependency.Create("demo", ">=1.0.0, <2.0.0");
        CargoPackageMetadata[] candidates = new CargoPackageMetadata[]
        {
            Metadata("demo", "1.0.0", "crates-io", false, new string[0]),
            Metadata("demo", "1.1.0-alpha.1", "crates-io", false, new string[0]),
            Metadata("demo", "1.2.0", "crates-io", true, new string[0])
        };
        CargoResolverOptions options = CargoResolverOptions.CreateDefault();
        options.SetIncludePrereleaseVersions(true);
        options.SetIncludeYankedVersions(true);

        CargoPackageMetadata[] filtered = CargoVersions.FilterCandidates(dependency, candidates, options);

        Assert.That(filtered.Length, Is.EqualTo(3));
        Assert.That(filtered[1].GetIdentity().GetVersionText(), Is.EqualTo("1.1.0-alpha.1"));
        Assert.That(filtered[2].IsYanked(), Is.True);
    }

    [Test]
    public void FilterCandidates_AllowsPrereleaseWhenRequirementNamesSameCore()
    {
        CargoDependency dependency = CargoDependency.Create("demo", ">=1.1.0-alpha.1, <1.1.0");
        CargoPackageMetadata[] candidates = new CargoPackageMetadata[]
        {
            Metadata("demo", "1.1.0-alpha.1", "crates-io", false, new string[0]),
            Metadata("demo", "1.1.0-alpha.2", "crates-io", false, new string[0]),
            Metadata("demo", "1.1.0", "crates-io", false, new string[0])
        };

        CargoPackageMetadata[] filtered = CargoVersions.FilterCandidates(
            dependency,
            candidates,
            CargoResolverOptions.CreateDefault());

        Assert.That(filtered.Length, Is.EqualTo(2));
        Assert.That(filtered[0].GetIdentity().GetVersionText(), Is.EqualTo("1.1.0-alpha.1"));
        Assert.That(filtered[1].GetIdentity().GetVersionText(), Is.EqualTo("1.1.0-alpha.2"));
    }

    [Test]
    public void FilterCandidates_AppliesSourceReplacementForDefaultRegistry()
    {
        CargoDependency dependency = CargoDependency.Create("demo", "1");
        CargoPackageMetadata[] candidates = new CargoPackageMetadata[]
        {
            Metadata("demo", "1.0.0", "crates-io", false, new string[0]),
            MetadataWithUrl("demo", "1.1.0", "mirror", "https://example.test/mirror-index", false, new string[0])
        };
        CargoResolverOptions options = CargoResolverOptions.CreateDefault();
        CargoRegistrySource mirror = CargoRegistrySource.Create("mirror", "https://example.test/mirror-index");
        options.SetSourceReplacements(new CargoSourceReplacement[]
        {
            CargoSourceReplacement.Create("crates-io", mirror)
        });

        CargoPackageMetadata[] filtered = CargoVersions.FilterCandidates(dependency, candidates, options);

        Assert.That(filtered.Length, Is.EqualTo(1));
        Assert.That(filtered[0].GetRegistrySource().GetName(), Is.EqualTo("mirror"));
        Assert.That(filtered[0].GetIdentity().GetVersionText(), Is.EqualTo("1.1.0"));
    }

    [Test]
    public void FilterCandidates_UsesDependencyRegistryNameAndConfiguredRegistrySource()
    {
        CargoDependency dependency = new CargoDependency(
            new CargoPackageName("demo"),
            "1",
            CargoDependencyKind.Normal,
            false,
            true,
            new string[0],
            "private",
            null);
        CargoPackageMetadata[] candidates = new CargoPackageMetadata[]
        {
            Metadata("demo", "1.0.0", "crates-io", false, new string[0]),
            MetadataWithUrl("demo", "1.1.0", "private", "https://example.test/private-index", false, new string[0])
        };
        CargoResolverOptions options = CargoResolverOptions.CreateDefault();
        options.SetRegistrySources(new CargoRegistrySource[]
        {
            CargoRegistrySource.Create("private", "https://example.test/private-index")
        });

        CargoPackageMetadata[] filtered = CargoVersions.FilterCandidates(dependency, candidates, options);

        Assert.That(filtered.Length, Is.EqualTo(1));
        Assert.That(filtered[0].GetRegistrySource().GetName(), Is.EqualTo("private"));
    }

    [Test]
    public void FilterCandidates_RequiresResolverSelectedFeaturesToExistOnCandidate()
    {
        CargoDependency dependency = CargoDependency.Create("demo", "1");
        CargoPackageMetadata[] candidates = new CargoPackageMetadata[]
        {
            Metadata("demo", "1.0.0", "crates-io", false, new string[] { "derive", "std" }),
            Metadata("demo", "1.1.0", "crates-io", false, new string[] { "std" })
        };
        CargoResolvedDependencyFeatures selectedFeatures = new CargoResolvedDependencyFeatures(
            new CargoPackageName("demo"),
            CargoDependencyKind.Normal,
            true,
            new string[] { "derive", "std" });

        CargoPackageMetadata[] filtered = CargoVersions.FilterCandidates(
            dependency,
            candidates,
            CargoResolverOptions.CreateDefault(),
            selectedFeatures);

        Assert.That(filtered.Length, Is.EqualTo(1));
        Assert.That(filtered[0].GetIdentity().GetVersionText(), Is.EqualTo("1.0.0"));
    }

    [Test]
    public void SelectHighestCandidate_ReturnsHighestRemainingCandidate()
    {
        CargoDependency dependency = CargoDependency.Create("demo", ">=1.0.0, <2.0.0");
        CargoPackageMetadata[] candidates = new CargoPackageMetadata[]
        {
            Metadata("demo", "1.0.0", "crates-io", false, new string[0]),
            Metadata("demo", "1.2.0", "crates-io", false, new string[0]),
            Metadata("demo", "1.1.0", "crates-io", true, new string[0])
        };

        CargoPackageMetadata? selected = CargoVersions.SelectHighestCandidate(
            dependency,
            candidates,
            CargoResolverOptions.CreateDefault());

        Assert.That(selected, Is.Not.Null);
        Assert.That(selected!.GetIdentity().GetVersionText(), Is.EqualTo("1.2.0"));
    }

    [Test]
    public void FilterCandidates_RejectsMismatchedSelectedFeaturePackage()
    {
        CargoDependency dependency = CargoDependency.Create("demo", "1");
        CargoPackageMetadata[] candidates = new CargoPackageMetadata[]
        {
            Metadata("demo", "1.0.0", "crates-io", false, new string[0])
        };
        CargoResolvedDependencyFeatures selectedFeatures = new CargoResolvedDependencyFeatures(
            new CargoPackageName("other"),
            CargoDependencyKind.Normal,
            false,
            new string[0]);

        Assert.That(
            delegate
            {
                CargoVersions.FilterCandidates(
                    dependency,
                    candidates,
                    CargoResolverOptions.CreateDefault(),
                    selectedFeatures);
            },
            Throws.TypeOf<ArgumentException>());
    }

    static CargoPackageMetadata Metadata(
        string packageName,
        string version,
        string sourceName,
        bool yanked,
        string[] featureNames)
    {
        return MetadataWithUrl(
            packageName,
            version,
            sourceName,
            "https://example.test/" + sourceName + "-index",
            yanked,
            featureNames);
    }

    static CargoPackageMetadata MetadataWithUrl(
        string packageName,
        string version,
        string sourceName,
        string sourceUrl,
        bool yanked,
        string[] featureNames)
    {
        return new CargoPackageMetadata(
            CargoPackageVersionIdentity.Create(packageName, version),
            new CargoDependency[0],
            Features(featureNames),
            CargoRegistrySource.Create(sourceName, sourceUrl),
            yanked);
    }

    static CargoFeature[] Features(string[] featureNames)
    {
        CargoFeature[] features = new CargoFeature[featureNames.Length];
        int index = 0;

        while (index < featureNames.Length)
        {
            features[index] = CargoFeature.Create(featureNames[index], new string[0]);
            index = index + 1;
        }

        return features;
    }
}
