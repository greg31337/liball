using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetPackageVersionResolverTests
{
    [Test]
    public void ResolveLowestApplicableVersion_SelectsLowestVersionAcrossConfiguredSources()
    {
        NuGetSourceIdentity nuget = Source("nuget.org");
        NuGetSourceIdentity internalSource = Source("internal");
        NuGetSourcePackageMetadata[] packages = new NuGetSourcePackageMetadata[]
        {
            Package("A", "1.1.0", nuget),
            Package("A", "1.0.0", internalSource),
            Package("A", "0.9.0", nuget)
        };

        NuGetSourcePackageMetadata? selected = NuGetVersions.ResolveLowestApplicableVersion(
            NuGetDependency.Create("A", "[1.0.0,)"),
            packages,
            Priorities(nuget, internalSource));

        AssertSelected(selected, "A", "1.0.0", internalSource);
    }

    [Test]
    public void ResolveLowestApplicableVersion_UsesSourcePriorityAsSameVersionTieBreaker()
    {
        NuGetSourceIdentity nuget = Source("nuget.org");
        NuGetSourceIdentity internalSource = Source("internal");
        NuGetSourcePackageMetadata[] packages = new NuGetSourcePackageMetadata[]
        {
            Package("A", "1.0.0", internalSource),
            Package("A", "1.0.0", nuget)
        };

        NuGetSourcePackageMetadata? selected = NuGetPackageVersionResolver.ResolveLowestApplicableVersion(
            NuGetDependency.Create("A", "[1.0.0,)"),
            packages,
            Priorities(nuget, internalSource));

        AssertSelected(selected, "A", "1.0.0", nuget);
    }

    [Test]
    public void ResolveLowestApplicableVersion_IgnoresDisabledAndUnconfiguredSources()
    {
        NuGetSourceIdentity enabled = Source("enabled");
        NuGetSourceIdentity disabled = Source("disabled");
        NuGetSourceIdentity unconfigured = Source("unconfigured");
        NuGetSourcePackageMetadata[] packages = new NuGetSourcePackageMetadata[]
        {
            Package("A", "1.0.0", disabled),
            Package("A", "1.1.0", unconfigured),
            Package("A", "1.2.0", enabled)
        };
        NuGetPackageSourcePriority[] priorities = new NuGetPackageSourcePriority[]
        {
            NuGetPackageSourcePriority.CreateEnabled(enabled, 0),
            NuGetPackageSourcePriority.CreateDisabled(disabled, 1)
        };

        NuGetSourcePackageMetadata? selected = NuGetPackageVersionResolver.ResolveLowestApplicableVersion(
            NuGetDependency.Create("A", "[1.0.0,)"),
            packages,
            priorities);

        AssertSelected(selected, "A", "1.2.0", enabled);
    }

    [Test]
    public void ResolveLowestApplicableVersion_DefaultPolicyExcludesPrereleaseWhileIncludeAllowsIt()
    {
        NuGetSourceIdentity source = Source("nuget.org");
        NuGetSourcePackageMetadata[] packages = new NuGetSourcePackageMetadata[]
        {
            Package("A", "1.1.0-alpha.1", source)
        };

        NuGetSourcePackageMetadata? defaultSelected = NuGetPackageVersionResolver.ResolveLowestApplicableVersion(
            NuGetDependency.Create("A", "[1.0.0,2.0.0)"),
            packages,
            Priorities(source));
        NuGetSourcePackageMetadata? includeSelected = NuGetPackageVersionResolver.ResolveLowestApplicableVersion(
            NuGetDependency.Create("A", "[1.0.0,2.0.0)"),
            packages,
            Priorities(source),
            VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.Include));

        Assert.That(defaultSelected, Is.Null);
        AssertSelected(includeSelected, "A", "1.1.0-alpha.1", source);
    }

    [Test]
    public void ResolveLowestApplicableVersion_AutoIfNoStableCandidateFallsBackToPrerelease()
    {
        NuGetSourceIdentity source = Source("nuget.org");
        NuGetSourcePackageMetadata prerelease = Package("A", "1.1.0-alpha.1", source);
        NuGetSourcePackageMetadata stable = Package("A", "1.0.0", source);

        NuGetSourcePackageMetadata? selectedWithStable = NuGetPackageVersionResolver.ResolveLowestApplicableVersion(
            NuGetDependency.Create("A", "[1.0.0,2.0.0)"),
            new NuGetSourcePackageMetadata[] { prerelease, stable },
            Priorities(source),
            VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.AutoIfNoStableCandidate));
        NuGetSourcePackageMetadata? selectedWithoutStable = NuGetPackageVersionResolver.ResolveLowestApplicableVersion(
            NuGetDependency.Create("A", "[1.0.0,2.0.0)"),
            new NuGetSourcePackageMetadata[] { prerelease },
            Priorities(source),
            VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.AutoIfNoStableCandidate));

        AssertSelected(selectedWithStable, "A", "1.0.0", source);
        AssertSelected(selectedWithoutStable, "A", "1.1.0-alpha.1", source);
    }

    [Test]
    public void ResolveFloatingVersion_SelectsHighestStableMatchForStableFloatingPattern()
    {
        NuGetSourceIdentity source = Source("nuget.org");

        NuGetSourcePackageMetadata? selected = NuGetVersions.ResolveFloatingVersion(
            "A",
            "1.*",
            new NuGetSourcePackageMetadata[]
            {
                Package("A", "1.0.0", source),
                Package("A", "1.2.0", source),
                Package("A", "1.3.0-alpha.1", source),
                Package("A", "2.0.0", source)
            },
            Priorities(source));

        AssertSelected(selected, "A", "1.2.0", source);
    }

    [Test]
    public void ResolveFloatingVersion_PrereleaseFloatingPatternIncludesPrereleaseCandidates()
    {
        NuGetSourceIdentity source = Source("nuget.org");

        NuGetSourcePackageMetadata? selected = NuGetPackageVersionResolver.ResolveFloatingVersion(
            "A",
            "1.*-*",
            new NuGetSourcePackageMetadata[]
            {
                Package("A", "1.1.0", source),
                Package("A", "1.2.0-alpha.1", source),
                Package("A", "1.2.0-beta.1", source),
                Package("A", "2.0.0-alpha.1", source)
            },
            Priorities(source));

        AssertSelected(selected, "A", "1.2.0-beta.1", source);
    }

    [Test]
    public void ResolveFloatingVersion_UsesSourcePriorityAsSameVersionTieBreaker()
    {
        NuGetSourceIdentity nuget = Source("nuget.org");
        NuGetSourceIdentity internalSource = Source("internal");

        NuGetSourcePackageMetadata? selected = NuGetPackageVersionResolver.ResolveFloatingVersion(
            "A",
            "1.*",
            new NuGetSourcePackageMetadata[]
            {
                Package("A", "1.2.0", internalSource),
                Package("A", "1.2.0", nuget)
            },
            Priorities(nuget, internalSource));

        AssertSelected(selected, "A", "1.2.0", nuget);
    }

    [Test]
    public void ResolveVersion_ReturnsNullWhenNoConfiguredSourceHasMatchingPackage()
    {
        NuGetSourceIdentity source = Source("nuget.org");

        NuGetSourcePackageMetadata? selected = NuGetPackageVersionResolver.ResolveLowestApplicableVersion(
            NuGetDependency.Create("A", "[2.0.0,)"),
            new NuGetSourcePackageMetadata[] { Package("A", "1.0.0", source) },
            Priorities(source));

        Assert.That(selected, Is.Null);
    }

    [Test]
    public void ResolveVersion_RejectsInvalidFloatingPatternsAndNullArrayElements()
    {
        NuGetSourceIdentity source = Source("nuget.org");

        Assert.That(
            delegate
            {
                NuGetPackageVersionResolver.ResolveFloatingVersion(
                    "A",
                    "1.*+build",
                    new NuGetSourcePackageMetadata[0],
                    Priorities(source));
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                NuGetPackageVersionResolver.ResolveLowestApplicableVersion(
                    NuGetDependency.Create("A", "[1.0.0,)"),
                    new NuGetSourcePackageMetadata[] { null! },
                    Priorities(source));
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                NuGetPackageVersionResolver.ResolveLowestApplicableVersion(
                    NuGetDependency.Create("A", "[1.0.0,)"),
                    new NuGetSourcePackageMetadata[0],
                    new NuGetPackageSourcePriority[] { null! });
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    static void AssertSelected(NuGetSourcePackageMetadata? metadata, string packageId, string version, NuGetSourceIdentity source)
    {
        System.ArgumentNullException.ThrowIfNull(metadata);
        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata!.GetIdentity().GetPackageId().MatchesText(packageId), Is.True);
        Assert.That(metadata.GetIdentity().GetNormalizedVersionText(), Is.EqualTo(version));
        Assert.That(metadata.GetSource().EqualsSource(source), Is.True);
    }

    static NuGetSourcePackageMetadata Package(string packageId, string version, NuGetSourceIdentity source)
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
            new NuGetDependencyGroup[0]);

        return NuGetSourcePackageMetadata.CreateAvailable(source, packageMetadata, NuGetListedState.Listed);
    }

    static NuGetSourceIdentity Source(string name)
    {
        return new NuGetSourceIdentity(name, "https://" + name + ".example.test/v3/index.json");
    }

    static NuGetPackageSourcePriority[] Priorities(NuGetSourceIdentity first)
    {
        return new NuGetPackageSourcePriority[]
        {
            NuGetPackageSourcePriority.CreateEnabled(first, 0)
        };
    }

    static NuGetPackageSourcePriority[] Priorities(NuGetSourceIdentity first, NuGetSourceIdentity second)
    {
        return new NuGetPackageSourcePriority[]
        {
            NuGetPackageSourcePriority.CreateEnabled(first, 0),
            NuGetPackageSourcePriority.CreateEnabled(second, 1)
        };
    }
}
