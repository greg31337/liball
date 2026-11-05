using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class RubyGemsResolverCompatibilityTests
{
    [Test]
    public void RequirementApi_CoversRubyGemsDefaultsExactInputsArraysAndPrereleaseDefaults()
    {
        RubyGemsRequirementList defaultRequirement = RubyGemsVersions.CreateDefaultRequirement();
        RubyGemsRequirementList defaultPrerelease = RubyGemsVersions.CreateDefaultPrereleaseRequirement();
        RubyGemsRequirementList exactRequirement = RubyGemsRequirementList.CreateFromVersionText("1.0");
        RubyGemsRequirementList arrayRequirement = RubyGemsVersions.CreateRequirementList(
            new string?[]?[]
            {
                new string[] { ">= 1.0, < 2.0" },
                new string?[] { "!= 1.5", null },
                null
            });

        Assert.That(defaultRequirement.ToNormalizedRequirementText(), Is.EqualTo(">= 0"));
        Assert.That(defaultRequirement.AllowsVersionText("1.0.0"), Is.True);
        Assert.That(defaultRequirement.AllowsVersionText("1.0.a"), Is.False);
        Assert.That(defaultPrerelease.ToNormalizedRequirementText(), Is.EqualTo(">= 0.a"));
        Assert.That(defaultPrerelease.AllowsVersionText("1.0.a"), Is.True);
        Assert.That(exactRequirement.IsExact(), Is.True);
        Assert.That(exactRequirement.AllowsVersionText("1.0.0"), Is.True);
        Assert.That(exactRequirement.AllowsVersionText("1.0.1"), Is.False);
        Assert.That(arrayRequirement.ToNormalizedRequirementText(), Is.EqualTo(">= 1.0, < 2.0, != 1.5"));
        Assert.That(arrayRequirement.AllowsVersionText("1.4.9"), Is.True);
        Assert.That(arrayRequirement.AllowsVersionText("1.5"), Is.False);
    }

    [Test]
    public void ResolverSelection_ChoosesHighestCompatibleVersionAndBacktracksWhenTransitivesFail()
    {
        RubyGemsSpecification app1 = Spec(
            "app",
            "1.0.0",
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rack", "~> 2.2.0") });
        RubyGemsSpecification app2 = Spec(
            "app",
            "2.0.0",
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rack", "~> 3.0") });
        RubyGemsSpecification rack227 = Spec("rack", "2.2.7", new RubyGemsDependency[0]);
        RubyGemsSpecification rack228 = Spec("rack", "2.2.8", new RubyGemsDependency[0]);
        RubyGemsSpecification rack230 = Spec("rack", "2.3.0", new RubyGemsDependency[0]);
        RubyGemsResolverOptions options = RubyGemsResolverOptions.CreateDefault();
        options.SetExclusions(new RubyGemsDependencyExclusion[] { RubyGemsDependencyExclusion.Create("rack", "= 2.2.8") });

        RubyGemsDependencyResolutionResult result = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("app", ">= 1.0, < 3.0") },
            new RubyGemsSpecification[] { app1, app2, rack227, rack228, rack230 },
            options);

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "app", "1.0.0", 0, true);
        AssertSelected(result, "rack", "2.2.7", 1, false);
    }

    [Test]
    public void PrereleaseActivation_RequiresExplicitPrereleaseRequirementOrIncludePolicy()
    {
        RubyGemsSpecification prerelease = Spec("rack", "2.0.0.pre", new RubyGemsDependency[0]);
        RubyGemsResolverOptions include = RubyGemsResolverOptions.CreateDefault();
        include.SetPrereleasePolicy(PrereleasePolicy.Include);

        RubyGemsDependencyResolutionResult defaultResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rack", ">= 0") },
            new RubyGemsSpecification[] { prerelease },
            RubyGemsResolverOptions.CreateDefault());
        RubyGemsDependencyResolutionResult explicitResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rack", ">= 2.0.0.pre") },
            new RubyGemsSpecification[] { prerelease },
            RubyGemsResolverOptions.CreateDefault());
        RubyGemsDependencyResolutionResult includeResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rack", ">= 0") },
            new RubyGemsSpecification[] { prerelease },
            include);

        Assert.That(defaultResult.IsFailure(), Is.True);
        Assert.That(defaultResult.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(RubyGemsResolverDiagnosticCodes.UnsatisfiedConstraints));
        AssertSelected(explicitResult, "rack", "2.0.0.pre", 0, true);
        AssertSelected(includeResult, "rack", "2.0.0.pre", 0, true);
    }

    [Test]
    public void PlatformMatching_UsesRubyFallbackAndNativeCompatibilityForResolverSelection()
    {
        RubyGemsSpecification rubySpec = SpecWithPlatform(
            "ffi",
            "1.0.0",
            RubyGemsPlatform.CreateRuby(),
            new RubyGemsDependency[0]);
        RubyGemsSpecification genericArm = SpecWithPlatform(
            "ffi",
            "1.0.0",
            new RubyGemsPlatform("arm-linux"),
            new RubyGemsDependency[0]);
        RubyGemsSpecification windows = SpecWithPlatform(
            "ffi",
            "1.0.0",
            new RubyGemsPlatform("x64-mingw-ucrt"),
            new RubyGemsDependency[0]);
        RubyGemsResolverOptions linuxArm = RubyGemsResolverOptions.CreateDefault();
        linuxArm.SetPlatforms(new RubyGemsPlatform[] { RubyGemsPlatform.CreateRuby(), new RubyGemsPlatform("armv7-linux") });

        RubyGemsDependencyResolutionResult result = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("ffi", "= 1.0.0") },
            new RubyGemsSpecification[] { rubySpec, genericArm, windows },
            linuxArm);

        Assert.That(RubyGemsVersions.PlatformMatches(new RubyGemsPlatform("arm-linux"), new RubyGemsPlatform("armv7-linux")), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetSelectedSpecification("ffi").GetSpecification().GetPlatform().GetValue(), Is.EqualTo("arm-linux"));
    }

    [Test]
    public void MultipleSources_FilterCandidatesAndUseConfiguredSourceOrder()
    {
        RubyGemsSource rubygems = RubyGemsSource.Create("rubygems", "https://rubygems.org");
        RubyGemsSource mirror = RubyGemsSource.Create("mirror", "https://example.test/mirror");
        RubyGemsSpecification mirrorSpec = SpecWithSource("rack", "2.2.8", new RubyGemsDependency[0], mirror);
        RubyGemsSpecification rubygemsSpec = SpecWithSource("rack", "2.2.8", new RubyGemsDependency[0], rubygems);
        RubyGemsResolverOptions mirrorFirst = RubyGemsResolverOptions.CreateDefault();
        mirrorFirst.SetSources(new RubyGemsSource[] { mirror, rubygems });
        RubyGemsResolverOptions mirrorOnly = RubyGemsResolverOptions.CreateDefault();
        mirrorOnly.SetSources(new RubyGemsSource[] { mirror });

        RubyGemsDependencyResolutionResult orderedResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rack", "= 2.2.8") },
            new RubyGemsSpecification[] { rubygemsSpec, mirrorSpec },
            mirrorFirst);
        RubyGemsDependencyResolutionResult filteredResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rack", "= 2.2.8") },
            new RubyGemsSpecification[] { rubygemsSpec },
            mirrorOnly);

        Assert.That(orderedResult.IsSuccess(), Is.True);
        Assert.That(orderedResult.GetSelectedSpecification("rack").GetSpecification().GetSource().GetName(), Is.EqualTo("mirror"));
        Assert.That(filteredResult.IsFailure(), Is.True);
        Assert.That(filteredResult.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(RubyGemsResolverDiagnosticCodes.UnsatisfiedConstraints));
    }

    [Test]
    public void ConflictDiagnostics_CoverMissingUnsatisfiedCyclesAndBacktrackingLimits()
    {
        RubyGemsDependencyResolutionResult missing = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("missing", ">= 0") },
            new RubyGemsSpecification[0],
            RubyGemsResolverOptions.CreateDefault());
        RubyGemsDependencyResolutionResult unsatisfied = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rack", "= 3.0.0") },
            new RubyGemsSpecification[] { Spec("rack", "2.2.8", new RubyGemsDependency[0]) },
            RubyGemsResolverOptions.CreateDefault());
        RubyGemsSpecification packageA = Spec(
            "a",
            "1.0.0",
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("b", "= 1.0.0") });
        RubyGemsSpecification packageB = Spec(
            "b",
            "1.0.0",
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("a", "= 1.0.0") });
        RubyGemsResolverOptions limited = RubyGemsResolverOptions.CreateDefault();
        limited.SetMaxBacktrackingSteps(1);
        RubyGemsDependencyResolutionResult cycle = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("a", "= 1.0.0") },
            new RubyGemsSpecification[] { packageA, packageB },
            RubyGemsResolverOptions.CreateDefault());
        RubyGemsDependencyResolutionResult backtracking = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("app", ">= 1.0, < 3.0") },
            new RubyGemsSpecification[]
            {
                Spec("app", "1.0.0", new RubyGemsDependency[0]),
                Spec("app", "2.0.0", new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("missing", "= 1.0.0") })
            },
            limited);

        Assert.That(missing.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(RubyGemsResolverDiagnosticCodes.MissingSpecificationMetadata));
        Assert.That(unsatisfied.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(RubyGemsResolverDiagnosticCodes.UnsatisfiedConstraints));
        Assert.That(cycle.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(RubyGemsResolverDiagnosticCodes.CyclicDependency));
        Assert.That(backtracking.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(RubyGemsResolverDiagnosticCodes.BacktrackingLimitExceeded));
    }

    static void AssertSelected(RubyGemsDependencyResolutionResult result, string name, string version, int depth, bool direct)
    {
        RubyGemsDependencyGraphNode node = result.GetSelectedSpecification(name);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(node.GetName(), Is.EqualTo(name));
        Assert.That(node.GetVersionText(), Is.EqualTo(version));
        Assert.That(node.GetDepth(), Is.EqualTo(depth));
        Assert.That(node.IsDirect(), Is.EqualTo(direct));
    }

    static RubyGemsSpecification Spec(string name, string version, RubyGemsDependency[] dependencies)
    {
        return RubyGemsSpecification.Create(
            name,
            version,
            RubyGemsPlatform.CreateRuby(),
            dependencies,
            RubyGemsSource.Create("rubygems", "https://rubygems.org"));
    }

    static RubyGemsSpecification SpecWithPlatform(
        string name,
        string version,
        RubyGemsPlatform platform,
        RubyGemsDependency[] dependencies)
    {
        return RubyGemsSpecification.Create(
            name,
            version,
            platform,
            dependencies,
            RubyGemsSource.Create("rubygems", "https://rubygems.org"));
    }

    static RubyGemsSpecification SpecWithSource(
        string name,
        string version,
        RubyGemsDependency[] dependencies,
        RubyGemsSource source)
    {
        return RubyGemsSpecification.Create(name, version, RubyGemsPlatform.CreateRuby(), dependencies, source);
    }
}
