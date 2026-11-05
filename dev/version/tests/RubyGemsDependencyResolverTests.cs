using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class RubyGemsDependencyResolverTests
{
    [Test]
    public void ResolveDependencies_CreatesGraphAndAppliesPessimisticConstraints()
    {
        RubyGemsSpecification rails = Spec(
            "rails",
            "7.1.0",
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rack", "~> 2.2.0") });
        RubyGemsSpecification rack229 = Spec("rack", "2.2.9", new RubyGemsDependency[0]);
        RubyGemsSpecification rack230 = Spec("rack", "2.3.0", new RubyGemsDependency[0]);

        RubyGemsDependencyResolutionResult result = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rails", "= 7.1.0") },
            new RubyGemsSpecification[] { rack230, rails, rack229 },
            RubyGemsResolverOptions.CreateDefault());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetNodeCount(), Is.EqualTo(2));
        AssertSelected(result, "rails", "7.1.0", 0, true);
        AssertSelected(result, "rack", "2.2.9", 1, false);
        Assert.That(result.GetEdgeCount(), Is.EqualTo(2));
        Assert.That(result.GetEdge(0).HasParent(), Is.False);
        Assert.That(result.GetEdge(1).HasParent(), Is.True);
        Assert.That(result.GetEdge(1).GetParentSpecification().MatchesName("rails"), Is.True);
        Assert.That(result.GetEdge(1).GetDependency().GetName(), Is.EqualTo("rack"));
    }

    [Test]
    public void ResolveDependencies_BacktracksToLowerCandidateWhenHighestCandidateFailsTransitively()
    {
        RubyGemsSpecification app1 = Spec(
            "app",
            "1.0.0",
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("helper", "= 1.0.0") });
        RubyGemsSpecification app2 = Spec(
            "app",
            "2.0.0",
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("helper", "= 2.0.0") });
        RubyGemsSpecification helper = Spec("helper", "1.0.0", new RubyGemsDependency[0]);

        RubyGemsDependencyResolutionResult result = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("app", ">= 1.0.0, < 3.0.0") },
            new RubyGemsSpecification[] { app1, app2, helper },
            RubyGemsResolverOptions.CreateDefault());

        Assert.That(result.IsSuccess(), Is.True);
        AssertSelected(result, "app", "1.0.0", 0, true);
        AssertSelected(result, "helper", "1.0.0", 1, false);
    }

    [Test]
    public void ResolveDependencies_ReportsConflictForDisjointTransitiveConstraints()
    {
        RubyGemsSpecification packageA = Spec(
            "a",
            "1.0.0",
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("c", "= 1.0.0") });
        RubyGemsSpecification packageB = Spec(
            "b",
            "1.0.0",
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("c", "= 2.0.0") });
        RubyGemsSpecification packageC1 = Spec("c", "1.0.0", new RubyGemsDependency[0]);
        RubyGemsSpecification packageC2 = Spec("c", "2.0.0", new RubyGemsDependency[0]);

        RubyGemsDependencyResolutionResult result = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[]
            {
                RubyGemsDependency.CreateRuntime("a", "= 1.0.0"),
                RubyGemsDependency.CreateRuntime("b", "= 1.0.0")
            },
            new RubyGemsSpecification[] { packageA, packageB, packageC1, packageC2 },
            RubyGemsResolverOptions.CreateDefault());

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetConflictCount(), Is.EqualTo(1));
        Assert.That(result.GetConflict(0).GetName(), Is.EqualTo("c"));
        Assert.That(result.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(RubyGemsResolverDiagnosticCodes.UnsatisfiedConstraints));
    }

    [Test]
    public void ResolveDependencies_ReportsMissingSpecificationMetadataAndCycles()
    {
        RubyGemsDependencyResolutionResult missingResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("missing", ">= 0") },
            new RubyGemsSpecification[0],
            RubyGemsResolverOptions.CreateDefault());

        RubyGemsSpecification packageA = Spec(
            "a",
            "1.0.0",
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("b", "= 1.0.0") });
        RubyGemsSpecification packageB = Spec(
            "b",
            "1.0.0",
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("a", "= 1.0.0") });
        RubyGemsDependencyResolutionResult cycleResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("a", "= 1.0.0") },
            new RubyGemsSpecification[] { packageA, packageB },
            RubyGemsResolverOptions.CreateDefault());

        Assert.That(missingResult.IsFailure(), Is.True);
        Assert.That(missingResult.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(RubyGemsResolverDiagnosticCodes.MissingSpecificationMetadata));
        Assert.That(cycleResult.IsFailure(), Is.True);
        Assert.That(cycleResult.GetConflict(0).GetName(), Is.EqualTo("a"));
        Assert.That(cycleResult.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(RubyGemsResolverDiagnosticCodes.CyclicDependency));
    }

    [Test]
    public void ResolveDependencies_HonorsDevelopmentDependencyOption()
    {
        RubyGemsSpecification app = Spec(
            "app",
            "1.0.0",
            new RubyGemsDependency[] { RubyGemsDependency.CreateDevelopment("devtool", "= 1.0.0") });
        RubyGemsResolverOptions includeDevelopment = RubyGemsResolverOptions.CreateDefault();
        includeDevelopment.SetIncludeDevelopmentDependencies(true);

        RubyGemsDependencyResolutionResult defaultResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("app", "= 1.0.0") },
            new RubyGemsSpecification[] { app },
            RubyGemsResolverOptions.CreateDefault());
        RubyGemsDependencyResolutionResult includeResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("app", "= 1.0.0") },
            new RubyGemsSpecification[] { app },
            includeDevelopment);

        Assert.That(defaultResult.IsSuccess(), Is.True);
        Assert.That(defaultResult.HasSelectedSpecification("devtool"), Is.False);
        Assert.That(includeResult.IsFailure(), Is.True);
        Assert.That(includeResult.GetConflict(0).GetName(), Is.EqualTo("devtool"));
    }

    [Test]
    public void ResolveDependencies_AppliesPrereleasePolicy()
    {
        RubyGemsSpecification stable = Spec("rack", "2.0.0", new RubyGemsDependency[0]);
        RubyGemsSpecification prerelease = Spec("rack", "2.1.0.pre", new RubyGemsDependency[0]);
        RubyGemsResolverOptions include = RubyGemsResolverOptions.CreateDefault();
        include.SetPrereleasePolicy(PrereleasePolicy.Include);
        RubyGemsResolverOptions exclude = RubyGemsResolverOptions.CreateDefault();
        exclude.SetPrereleasePolicy(PrereleasePolicy.Exclude);
        RubyGemsResolverOptions auto = RubyGemsResolverOptions.CreateDefault();
        auto.SetPrereleasePolicy(PrereleasePolicy.AutoIfNoStableCandidate);

        RubyGemsDependencyResolutionResult defaultResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rack", ">= 0") },
            new RubyGemsSpecification[] { prerelease, stable },
            RubyGemsResolverOptions.CreateDefault());
        RubyGemsDependencyResolutionResult includeResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rack", ">= 0") },
            new RubyGemsSpecification[] { prerelease, stable },
            include);
        RubyGemsDependencyResolutionResult explicitResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rack", ">= 2.1.0.pre") },
            new RubyGemsSpecification[] { prerelease, stable },
            exclude);
        RubyGemsDependencyResolutionResult autoResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rack", ">= 0") },
            new RubyGemsSpecification[] { prerelease },
            auto);

        AssertSelected(defaultResult, "rack", "2.0.0", 0, true);
        AssertSelected(includeResult, "rack", "2.1.0.pre", 0, true);
        Assert.That(explicitResult.IsFailure(), Is.True);
        Assert.That(explicitResult.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(RubyGemsResolverDiagnosticCodes.UnsatisfiedConstraints));
        AssertSelected(autoResult, "rack", "2.1.0.pre", 0, true);
    }

    [Test]
    public void ResolveDependencies_UsesPlatformPreferenceAndSourceOrder()
    {
        RubyGemsSource rubygems = RubyGemsSource.Create("rubygems", "https://rubygems.org");
        RubyGemsSource mirror = RubyGemsSource.Create("mirror", "https://example.test/mirror");
        RubyGemsSpecification nativeSpec = SpecWithPlatformAndSource(
            "native",
            "1.0.0",
            new RubyGemsPlatform("x86_64-linux"),
            new RubyGemsDependency[0],
            rubygems);
        RubyGemsSpecification rubySpec = SpecWithPlatformAndSource(
            "native",
            "1.0.0",
            RubyGemsPlatform.CreateRuby(),
            new RubyGemsDependency[0],
            rubygems);
        RubyGemsSpecification mirrorSpec = SpecWithPlatformAndSource(
            "mirror-gem",
            "1.0.0",
            RubyGemsPlatform.CreateRuby(),
            new RubyGemsDependency[0],
            mirror);
        RubyGemsSpecification rubygemsSpec = SpecWithPlatformAndSource(
            "mirror-gem",
            "1.0.0",
            RubyGemsPlatform.CreateRuby(),
            new RubyGemsDependency[0],
            rubygems);
        RubyGemsResolverOptions platformOptions = RubyGemsResolverOptions.CreateDefault();
        platformOptions.SetPlatforms(new RubyGemsPlatform[] { RubyGemsPlatform.CreateRuby(), new RubyGemsPlatform("x86_64-linux") });
        RubyGemsResolverOptions sourceOptions = RubyGemsResolverOptions.CreateDefault();
        sourceOptions.SetSources(new RubyGemsSource[] { mirror, rubygems });

        RubyGemsDependencyResolutionResult platformResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("native", "= 1.0.0") },
            new RubyGemsSpecification[] { rubySpec, nativeSpec },
            platformOptions);
        RubyGemsDependencyResolutionResult sourceResult = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("mirror-gem", "= 1.0.0") },
            new RubyGemsSpecification[] { rubygemsSpec, mirrorSpec },
            sourceOptions);

        Assert.That(platformResult.GetSelectedSpecification("native").GetSpecification().GetPlatform().GetValue(), Is.EqualTo("x86_64-linux"));
        Assert.That(sourceResult.GetSelectedSpecification("mirror-gem").GetSpecification().GetSource().GetName(), Is.EqualTo("mirror"));
    }

    [Test]
    public void ResolveDependencies_ExcludesConfiguredSpecifications()
    {
        RubyGemsSpecification rack227 = Spec("rack", "2.2.7", new RubyGemsDependency[0]);
        RubyGemsSpecification rack228 = Spec("rack", "2.2.8", new RubyGemsDependency[0]);
        RubyGemsResolverOptions options = RubyGemsResolverOptions.CreateDefault();
        RubyGemsDependencyExclusion[] exclusions = new RubyGemsDependencyExclusion[]
        {
            RubyGemsDependencyExclusion.Create("rack", "= 2.2.8")
        };
        options.SetExclusions(exclusions);
        exclusions[0] = RubyGemsDependencyExclusion.Create("rack");

        RubyGemsDependencyResolutionResult result = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("rack", "~> 2.2.0") },
            new RubyGemsSpecification[] { rack227, rack228 },
            options);

        Assert.That(options.GetExclusionCount(), Is.EqualTo(1));
        Assert.That(options.GetExclusion(0).GetRequirements().ToNormalizedRequirementText(), Is.EqualTo("= 2.2.8"));
        AssertSelected(result, "rack", "2.2.7", 0, true);
    }

    [Test]
    public void ResolveDependencies_StopsWhenBacktrackingLimitIsExceeded()
    {
        RubyGemsSpecification app1 = Spec(
            "app",
            "1.0.0",
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("helper", "= 1.0.0") });
        RubyGemsSpecification app2 = Spec(
            "app",
            "2.0.0",
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("helper", "= 2.0.0") });
        RubyGemsSpecification helper = Spec("helper", "1.0.0", new RubyGemsDependency[0]);
        RubyGemsResolverOptions options = RubyGemsResolverOptions.CreateDefault();
        options.SetMaxBacktrackingSteps(1);

        RubyGemsDependencyResolutionResult result = RubyGemsVersions.ResolveDependencies(
            new RubyGemsDependency[] { RubyGemsDependency.CreateRuntime("app", ">= 1.0.0, < 3.0.0") },
            new RubyGemsSpecification[] { app1, app2, helper },
            options);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetConflict(0).GetDiagnosticCode(), Is.EqualTo(RubyGemsResolverDiagnosticCodes.BacktrackingLimitExceeded));
    }

    [Test]
    public void ResolveDependencies_RejectsNullInputElements()
    {
        Assert.That(
            delegate
            {
                RubyGemsVersions.ResolveDependencies(
                    new RubyGemsDependency[] { null! },
                    new RubyGemsSpecification[0],
                    RubyGemsResolverOptions.CreateDefault());
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                RubyGemsVersions.ResolveDependencies(
                    new RubyGemsDependency[0],
                    new RubyGemsSpecification[] { null! },
                    RubyGemsResolverOptions.CreateDefault());
            },
            Throws.TypeOf<ArgumentException>());
    }

    static void AssertSelected(RubyGemsDependencyResolutionResult result, string name, string version, int depth, bool direct)
    {
        RubyGemsDependencyGraphNode node = result.GetSelectedSpecification(name);

        Assert.That(node.GetName(), Is.EqualTo(name));
        Assert.That(node.GetVersionText(), Is.EqualTo(version));
        Assert.That(node.GetDepth(), Is.EqualTo(depth));
        Assert.That(node.IsDirect(), Is.EqualTo(direct));
    }

    static RubyGemsSpecification Spec(string name, string version, RubyGemsDependency[] dependencies)
    {
        return SpecWithPlatformAndSource(
            name,
            version,
            RubyGemsPlatform.CreateRuby(),
            dependencies,
            RubyGemsSource.Create("rubygems", "https://rubygems.org"));
    }

    static RubyGemsSpecification SpecWithPlatformAndSource(
        string name,
        string version,
        RubyGemsPlatform platform,
        RubyGemsDependency[] dependencies,
        RubyGemsSource source)
    {
        return RubyGemsSpecification.Create(name, version, platform, dependencies, source);
    }
}
