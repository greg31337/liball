using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetRestoreCompatibilityTests
{
    [Test]
    public void ResolveDependencies_FiltersDependencyGroupsByProjectTargetFramework()
    {
        NuGetSourceIdentity source = Source("nuget");
        NuGetSourcePackageMetadata packageA = Package(
            "A",
            "1.0.0",
            source,
            new NuGetDependencyGroup[]
            {
                Group("netstandard2.0", "StandardOnly"),
                Group("net8.0", "NetOnly"),
                NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[] { Dependency("AnyOnly") })
            });
        NuGetSourcePackageMetadata netOnly = Package("NetOnly", "1.0.0", source, new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata standardOnly = Package("StandardOnly", "1.0.0", source, new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata anyOnly = Package("AnyOnly", "1.0.0", source, new NuGetDependencyGroup[0]);

        NuGetDependencyResolutionResult result = NuGetVersions.ResolveDependencies(
            new NuGetDependency[] { NuGetDependency.Create("A", "[1.0.0]") },
            new NuGetSourcePackageMetadata[] { packageA, netOnly, standardOnly, anyOnly },
            "net8.0");

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "A", "1.0.0");
        AssertSelected(result, "NetOnly", "1.0.0");
        Assert.That(result.HasSelectedPackage("StandardOnly"), Is.False);
        Assert.That(result.HasSelectedPackage("AnyOnly"), Is.False);
        Assert.That(result.GetEdgeCount(), Is.EqualTo(2));
    }

    [Test]
    public void ResolveDependencies_UsesUntargetedGroupWhenNoTargetedGroupIsCompatible()
    {
        NuGetSourceIdentity source = Source("nuget");
        NuGetSourcePackageMetadata packageA = Package(
            "A",
            "1.0.0",
            source,
            new NuGetDependencyGroup[]
            {
                Group("net9.0", "FutureOnly"),
                NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[] { Dependency("Compatible") })
            });
        NuGetSourcePackageMetadata compatible = Package("Compatible", "1.0.0", source, new NuGetDependencyGroup[0]);

        NuGetDependencyResolutionResult result = NuGetDependencyGraphResolver.Resolve(
            new NuGetDependency[] { NuGetDependency.Create("A", "[1.0.0]") },
            new NuGetSourcePackageMetadata[] { packageA, compatible },
            "net8.0");

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "Compatible", "1.0.0");
        Assert.That(result.HasSelectedPackage("FutureOnly"), Is.False);
    }

    [Test]
    public void ResolveDependencies_PreservesNearestDependencyResolutionRules()
    {
        NuGetSourceIdentity source = Source("nuget");
        NuGetSourcePackageMetadata packageA = Package(
            "A",
            "1.0.0",
            source,
            new NuGetDependencyGroup[] { NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[] { Dependency("B", "[1.0.0]") }) });
        NuGetSourcePackageMetadata packageB = Package(
            "B",
            "1.0.0",
            source,
            new NuGetDependencyGroup[] { NuGetDependencyGroup.CreateAnyFramework(new NuGetDependency[] { Dependency("C", "[2.0.0]") }) });
        NuGetSourcePackageMetadata packageC1 = Package("C", "1.0.0", source, new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata packageC2 = Package("C", "2.0.0", source, new NuGetDependencyGroup[0]);

        NuGetDependencyResolutionResult result = NuGetVersions.ResolveDependencies(
            new NuGetDependency[]
            {
                NuGetDependency.Create("A", "[1.0.0]"),
                NuGetDependency.Create("C", "[1.0.0]")
            },
            new NuGetSourcePackageMetadata[] { packageA, packageB, packageC1, packageC2 });

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "C", "1.0.0");
        Assert.That(result.GetSelectedPackage("C").IsDirect(), Is.True);
    }

    [Test]
    public void ResolveVersion_UsesSourceOrderingForSameVersionCandidates()
    {
        NuGetSourceIdentity nuget = Source("nuget");
        NuGetSourceIdentity internalSource = Source("internal");
        NuGetSourceIdentity disabled = Source("disabled");

        NuGetSourcePackageMetadata? selected = NuGetVersions.ResolveLowestApplicableVersion(
            NuGetDependency.Create("A", "[1.0.0,)"),
            new NuGetSourcePackageMetadata[]
            {
                Package("A", "1.0.0", internalSource, new NuGetDependencyGroup[0]),
                Package("A", "1.0.0", disabled, new NuGetDependencyGroup[0]),
                Package("A", "1.0.0", nuget, new NuGetDependencyGroup[0])
            },
            new NuGetPackageSourcePriority[]
            {
                NuGetPackageSourcePriority.CreateEnabled(nuget, 0),
                NuGetPackageSourcePriority.CreateEnabled(internalSource, 1),
                NuGetPackageSourcePriority.CreateDisabled(disabled, 2)
            });

        AssertSourcePackage(selected, "A", "1.0.0", nuget);
    }

    [Test]
    public void ResolveVersion_AppliesFloatingVersionAndPrereleasePolicyRules()
    {
        NuGetSourceIdentity source = Source("nuget");
        NuGetSourcePackageMetadata stable = Package("A", "1.1.0", source, new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata prerelease = Package("A", "1.2.0-beta.1", source, new NuGetDependencyGroup[0]);

        NuGetSourcePackageMetadata? stableFloating = NuGetVersions.ResolveFloatingVersion(
            "A",
            "1.*",
            new NuGetSourcePackageMetadata[] { stable, prerelease, Package("A", "2.0.0", source, new NuGetDependencyGroup[0]) },
            Priorities(source));
        NuGetSourcePackageMetadata? prereleaseFloating = NuGetVersions.ResolveFloatingVersion(
            "A",
            "1.*-*",
            new NuGetSourcePackageMetadata[] { stable, prerelease },
            Priorities(source));
        NuGetSourcePackageMetadata? defaultPrerelease = NuGetVersions.ResolveLowestApplicableVersion(
            NuGetDependency.Create("A", "[1.0.0,2.0.0)"),
            new NuGetSourcePackageMetadata[] { prerelease },
            Priorities(source));
        NuGetSourcePackageMetadata? autoPrerelease = NuGetVersions.ResolveLowestApplicableVersion(
            NuGetDependency.Create("A", "[1.0.0,2.0.0)"),
            new NuGetSourcePackageMetadata[] { prerelease },
            Priorities(source),
            VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.AutoIfNoStableCandidate));

        AssertSourcePackage(stableFloating, "A", "1.1.0", source);
        AssertSourcePackage(prereleaseFloating, "A", "1.2.0-beta.1", source);
        Assert.That(defaultPrerelease, Is.Null);
        AssertSourcePackage(autoPrerelease, "A", "1.2.0-beta.1", source);
    }

    [Test]
    public void PackageMetadataRules_ParseNuspecAndRejectStrictSourceMetadata()
    {
        NuGetPackageMetadata metadata = NuGetVersions.ParsePackageMetadata(CreateNuspec());
        NuGetDependencyGroup? selectedGroup = NuGetVersions.SelectBestDependencyGroup("net8.0", metadata);
        NuGetSourcePackageMetadata risky = new NuGetSourcePackageMetadata(
            Source("nuget"),
            metadata,
            NuGetListedState.Unlisted,
            NuGetPackageAvailability.Available,
            NuGetPackageDeprecationMetadata.CreateDeprecated(
                new NuGetPackageDeprecationReason[] { NuGetPackageDeprecationReason.Legacy },
                "Use a newer package.",
                null),
            new NuGetPackageVulnerabilityMetadata[]
            {
                NuGetPackageVulnerabilityMetadata.Create(NuGetPackageVulnerabilitySeverity.High, "https://advisories.example.test/a")
            });

        Assert.That(metadata.GetIdentity().GetPackageId().MatchesText("Restore.Package"), Is.True);
        Assert.That(metadata.GetIdentity().GetNormalizedVersionText(), Is.EqualTo("1.0.0"));
        Assert.That(metadata.GetMinClientVersion(), Is.EqualTo("6.0"));
        Assert.That(metadata.IsDevelopmentDependency(), Is.True);
        Assert.That(selectedGroup, Is.Not.Null);
        Assert.That(selectedGroup!.GetTargetFramework(), Is.EqualTo("net8.0"));
        Assert.That(selectedGroup.GetDependency(0).GetPackageId().MatchesText("Runtime.Dependency"), Is.True);
        Assert.That(NuGetPackageRejectionPolicy.CreateStrict().ShouldReject(risky), Is.True);
    }

    [Test]
    public void ResolveDependencies_RejectsInvalidProjectTargetFramework()
    {
        Assert.That(
            delegate
            {
                NuGetVersions.ResolveDependencies(
                    new NuGetDependency[0],
                    new NuGetSourcePackageMetadata[0],
                    "not-a-framework");
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    static NuGetDependency Dependency(string packageId)
    {
        return NuGetDependency.CreateAnyVersion(packageId);
    }

    static NuGetDependency Dependency(string packageId, string range)
    {
        return NuGetDependency.Create(packageId, range);
    }

    static NuGetDependencyGroup Group(string targetFramework, string dependencyPackageId)
    {
        return NuGetDependencyGroup.Create(targetFramework, new NuGetDependency[] { Dependency(dependencyPackageId) });
    }

    static NuGetSourcePackageMetadata Package(
        string packageId,
        string version,
        NuGetSourceIdentity source,
        NuGetDependencyGroup[] dependencyGroups)
    {
        NuGetPackageMetadata metadata = new NuGetPackageMetadata(
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
            dependencyGroups);

        return NuGetSourcePackageMetadata.CreateAvailable(source, metadata, NuGetListedState.Listed);
    }

    static NuGetSourceIdentity Source(string name)
    {
        return new NuGetSourceIdentity(name, "https://" + name + ".example.test/v3/index.json");
    }

    static NuGetPackageSourcePriority[] Priorities(NuGetSourceIdentity source)
    {
        return new NuGetPackageSourcePriority[]
        {
            NuGetPackageSourcePriority.CreateEnabled(source, 0)
        };
    }

    static void AssertSelected(NuGetDependencyResolutionResult result, string packageId, string version)
    {
        NuGetDependencyGraphNode node = result.GetSelectedPackage(packageId);

        Assert.That(node.GetIdentity().GetPackageId().MatchesText(packageId), Is.True);
        Assert.That(node.GetIdentity().GetNormalizedVersionText(), Is.EqualTo(version));
    }

    static void AssertSourcePackage(
        NuGetSourcePackageMetadata? packageMetadata,
        string packageId,
        string version,
        NuGetSourceIdentity source)
    {
        System.ArgumentNullException.ThrowIfNull(packageMetadata);
        Assert.That(packageMetadata, Is.Not.Null);
        Assert.That(packageMetadata!.GetIdentity().GetPackageId().MatchesText(packageId), Is.True);
        Assert.That(packageMetadata.GetIdentity().GetNormalizedVersionText(), Is.EqualTo(version));
        Assert.That(packageMetadata.GetSource().EqualsSource(source), Is.True);
    }

    static string CreateNuspec()
    {
        return """
            <?xml version="1.0" encoding="utf-8"?>
            <package>
              <metadata minClientVersion="6.0">
                <id>Restore.Package</id>
                <version>1.0.0</version>
                <authors>Example</authors>
                <description>Restore compatibility package.</description>
                <developmentDependency>true</developmentDependency>
                <dependencies>
                  <group targetFramework="netstandard2.0">
                    <dependency id="Compile.Dependency" version="[1.0.0,)" />
                  </group>
                  <group targetFramework="net8.0">
                    <dependency id="Runtime.Dependency" version="[2.0.0,)" />
                  </group>
                </dependencies>
              </metadata>
            </package>
            """;
    }
}
