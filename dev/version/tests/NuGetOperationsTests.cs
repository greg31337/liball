using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetOperationsTests
{
    [Test]
    public void VersionOperations_ParseNormalizeAndCompareUsingNuGetRules()
    {
        INuGetResolution operations = new NuGetOperations();

        VersionParseResult parsed = operations.ParseVersion("01.002.0003.0000-ALPHA+build.5");
        VersionParseResult invalid = operations.ParseVersion("1..2");

        Assert.That(parsed.success, Is.True);
        Assert.That(parsed.dialect, Is.EqualTo(VersionDialect.NuGet));
        Assert.That(parsed.version, Is.Not.Null);
        Assert.That(parsed.version!.numericComponents, Is.EqualTo(new string[] { "01", "002", "0003" }));
        Assert.That(parsed.version.revisionComponentPresent, Is.True);
        Assert.That(parsed.version.revisionComponent, Is.EqualTo("0000"));
        Assert.That(operations.NormalizeVersion(parsed.sourceText), Is.EqualTo("1.2.3-ALPHA"));
        Assert.That(operations.NormalizeVersion("invalid"), Is.Empty);
        Assert.That(operations.CompareVersions("1.0.0-ALPHA", "1.0.0-alpha"), Is.Zero);
        Assert.That(operations.CompareVersions("1.0.0-beta", "1.0.0-RC"), Is.LessThan(0));
        Assert.That(invalid.success, Is.False);
        Assert.That(invalid.diagnostics, Is.Not.Empty);
    }

    [Test]
    public void RangeOperations_ParseIntervalsAndFloatingPrereleasePatterns()
    {
        INuGetResolution operations = new NuGetOperations();

        VersionRangeParseResult interval = operations.ParseRange("[1.0,2.0)");
        VersionRangeParseResult malformed = operations.ParseRange("[2.0,1.0]");
        NuGetFloatingVersion floating = operations.ParseFloatingVersion("1.2.3-alpha.*");
        NuGetFloatingVersion invalidFloating = operations.ParseFloatingVersion("1.*+build");

        Assert.That(interval.success, Is.True);
        Assert.That(interval.syntax, Is.EqualTo(RangeSyntax.NuGet));
        Assert.That(interval.range, Is.Not.Null);
        Assert.That(malformed.success, Is.False);
        Assert.That(malformed.diagnostics, Is.Not.Empty);
        Assert.That(floating.valid, Is.True);
        Assert.That(floating.numericPrecision, Is.EqualTo(3));
        Assert.That(floating.includePrerelease, Is.True);
        Assert.That(floating.prereleaseAny, Is.False);
        Assert.That(floating.prereleasePrefix, Is.EqualTo(new string[] { "alpha" }));
        Assert.That(invalidFloating.valid, Is.False);
    }

    [Test]
    public void CandidateOperations_SelectLowestAndHighestWithSourceAndRejectionPolicies()
    {
        INuGetResolution operations = new NuGetOperations();
        NuGetSourceIdentity primary = Source("primary");
        NuGetSourceIdentity secondary = Source("secondary");
        NuGetPackageSourcePriority[] priorities = Priorities(primary, secondary);
        NuGetSourcePackageMetadata unlisted = Package(
            operations,
            "Example",
            "1.0.0",
            secondary,
            NuGetListedState.Unlisted,
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata stable = Package(
            operations,
            "Example",
            "1.1.0",
            primary,
            NuGetListedState.Listed,
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata prerelease = Package(
            operations,
            "Example",
            "1.2.0-beta.2",
            primary,
            NuGetListedState.Listed,
            new NuGetDependencyGroup[0]);
        NuGetDependency dependency = Dependency(operations, "Example", "[1.0.0,2.0.0)");

        NuGetSourcePackageMetadata? lowest = operations.ResolveLowestApplicableVersion(
            dependency,
            new NuGetSourcePackageMetadata[] { stable, unlisted, prerelease },
            priorities);
        NuGetPackageRejectionPolicy rejectionPolicy = new NuGetPackageRejectionPolicy();
        rejectionPolicy.rejectUnlistedPackages = true;
        NuGetSourcePackageMetadata? acceptedLowest = operations.ResolveLowestApplicableVersionWithPolicies(
            dependency,
            new NuGetSourcePackageMetadata[] { stable, unlisted, prerelease },
            priorities,
            rejectionPolicy,
            new VersionSelectionOptions());
        NuGetSourcePackageMetadata? highest = operations.ResolveHighestFloatingVersion(
            "example",
            "1.*-*",
            new NuGetSourcePackageMetadata[] { stable, unlisted, prerelease },
            priorities);

        Assert.That(lowest, Is.SameAs(unlisted));
        Assert.That(acceptedLowest, Is.SameAs(stable));
        Assert.That(highest, Is.SameAs(prerelease));
    }

    [Test]
    public void MetadataOperations_ParsePackageIdentityAndDependencyGroups()
    {
        INuGetResolution operations = new NuGetOperations();
        string nuspec = @"<package>
  <metadata>
    <id>Example.Package</id>
    <version>01.002+build.7</version>
    <authors>Example Authors</authors>
    <description>Example package</description>
    <dependencies>
      <group targetFramework=""net8.0"">
        <dependency id=""Dependency.A"" version=""[2.0,3.0)"" />
      </group>
    </dependencies>
  </metadata>
</package>";

        NuGetPackageMetadata metadata = operations.ParsePackageMetadata(nuspec);
        NuGetDependencyGroup[] groups = operations.ParseDependencyGroups(nuspec);

        Assert.That(metadata.identity.packageId.normalizedValue, Is.EqualTo("example.package"));
        Assert.That(metadata.identity.normalizedVersionText, Is.EqualTo("1.2.0"));
        Assert.That(metadata.authors, Is.EqualTo("Example Authors"));
        Assert.That(metadata.description, Is.EqualTo("Example package"));
        Assert.That(groups, Has.Length.EqualTo(1));
        Assert.That(groups[0].targetFramework, Is.EqualTo("net8.0"));
        Assert.That(groups[0].dependencies, Has.Length.EqualTo(1));
        Assert.That(groups[0].dependencies[0].packageId.normalizedValue, Is.EqualTo("dependency.a"));
    }

    [Test]
    public void TargetFrameworkOperations_ParseCompatibilityFallbackAndBestGroup()
    {
        INuGetResolution operations = new NuGetOperations();
        NuGetTargetFrameworkParseResult project = operations.ParseTargetFramework("net8.0-windows10.0.19041");
        NuGetTargetFrameworkParseResult candidate = operations.ParseTargetFramework("net8.0-windows10.0");
        NuGetTargetFrameworkFallbackParseResult fallback = operations.ParseTargetFrameworkFallbackList(
            "net48;netstandard2.0");
        NuGetDependencyGroup any = new NuGetDependencyGroup(null, new NuGetDependency[0]);
        NuGetDependencyGroup compatible = new NuGetDependencyGroup(
            "netstandard2.0",
            new NuGetDependency[0]);

        Assert.That(project.success, Is.True);
        Assert.That(candidate.success, Is.True);
        Assert.That(fallback.success, Is.True);
        Assert.That(fallback.fallbackList, Is.Not.Null);
        Assert.That(fallback.fallbackList!.frameworks, Has.Length.EqualTo(2));
        Assert.That(operations.IsTargetFrameworkCompatible("net8.0", "netstandard2.1"), Is.True);
        Assert.That(
            operations.IsTargetFrameworkCompatibleWithFallback("net8.0", "net48", "net48"),
            Is.True);
        Assert.That(
            operations.AreTargetFrameworksCompatible(project.framework!, candidate.framework!),
            Is.True);
        Assert.That(
            operations.SelectBestDependencyGroup(
                "net8.0",
                new NuGetDependencyGroup[] { any, compatible }),
            Is.SameAs(compatible));
    }

    [Test]
    public void DependencyResolution_AppliesSourcePriorityBeforeBuildingGraph()
    {
        INuGetResolution operations = new NuGetOperations();
        NuGetSourceIdentity primary = Source("primary");
        NuGetSourceIdentity secondary = Source("secondary");
        NuGetDependency root = Dependency(operations, "Root", "[1.0.0]");
        NuGetDependency transitive = Dependency(operations, "Transitive", "[2.0.0]");
        NuGetDependencyGroup[] primaryGroups = new NuGetDependencyGroup[]
        {
            new NuGetDependencyGroup(null, new NuGetDependency[] { transitive })
        };
        NuGetSourcePackageMetadata primaryRoot = Package(
            operations,
            "Root",
            "1.0.0",
            primary,
            NuGetListedState.Listed,
            primaryGroups);
        NuGetSourcePackageMetadata secondaryRoot = Package(
            operations,
            "Root",
            "1.0.0",
            secondary,
            NuGetListedState.Listed,
            new NuGetDependencyGroup[0]);
        NuGetSourcePackageMetadata transitivePackage = Package(
            operations,
            "Transitive",
            "2.0.0",
            primary,
            NuGetListedState.Listed,
            new NuGetDependencyGroup[0]);

        NuGetDependencyResolutionResult result = operations.ResolveDependencies(
            new NuGetDependency[] { root },
            new NuGetSourcePackageMetadata[] { secondaryRoot, primaryRoot, transitivePackage },
            Priorities(primary, secondary));

        Assert.That(result.conflicts, Is.Empty);
        Assert.That(result.nodes, Has.Length.EqualTo(2));
        Assert.That(result.nodes[0].packageMetadata, Is.SameAs(primaryRoot));
        Assert.That(result.nodes[1].packageMetadata, Is.SameAs(transitivePackage));
    }

    static NuGetDependency Dependency(INuGetResolution operations, string packageId, string rangeText)
    {
        VersionRangeParseResult range = operations.ParseRange(rangeText);
        Assert.That(range.success, Is.True);
        Assert.That(range.range, Is.Not.Null);
        return new NuGetDependency(new NuGetPackageId(packageId), range.range!, rangeText);
    }

    static NuGetSourcePackageMetadata Package(
        INuGetResolution operations,
        string packageId,
        string versionText,
        NuGetSourceIdentity source,
        NuGetListedState listedState,
        NuGetDependencyGroup[] groups)
    {
        VersionParseResult parsed = operations.ParseVersion(versionText);
        Assert.That(parsed.success, Is.True);
        Assert.That(parsed.version, Is.Not.Null);
        NuGetPackageMetadata metadata = new NuGetPackageMetadata(
            new NuGetPackageVersionIdentity(new NuGetPackageId(packageId), parsed.version!),
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
            groups);

        return new NuGetSourcePackageMetadata(
            source,
            metadata,
            listedState,
            NuGetPackageAvailability.Available,
            new NuGetPackageDeprecationMetadata(
                false,
                new NuGetPackageDeprecationReason[0],
                "",
                null),
            new NuGetPackageVulnerabilityMetadata[0]);
    }

    static NuGetSourceIdentity Source(string name)
    {
        return new NuGetSourceIdentity(name, "https://" + name + ".example.test/v3/index.json");
    }

    static NuGetPackageSourcePriority[] Priorities(
        NuGetSourceIdentity primary,
        NuGetSourceIdentity secondary)
    {
        return new NuGetPackageSourcePriority[]
        {
            new NuGetPackageSourcePriority(primary, 0, true),
            new NuGetPackageSourcePriority(secondary, 1, true)
        };
    }
}
