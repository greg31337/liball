using System;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class RubyGemsMetadataModelTests
{
    [Test]
    public void Platform_ParsesRubyUniversalAndNativePlatformParts()
    {
        RubyGemsPlatform ruby = RubyGemsPlatform.CreateRuby();
        RubyGemsPlatform universal = RubyGemsPlatform.CreateUniversal("java");
        RubyGemsPlatform native = new RubyGemsPlatform("x86_64-linux-musl");

        Assert.That(ruby.GetValue(), Is.EqualTo("ruby"));
        Assert.That(ruby.IsRuby(), Is.True);
        Assert.That(ruby.GetOs(), Is.EqualTo("ruby"));
        Assert.That(universal.IsUniversal(), Is.True);
        Assert.That(universal.GetCpu(), Is.EqualTo("universal"));
        Assert.That(universal.GetOs(), Is.EqualTo("java"));
        Assert.That(native.GetCpu(), Is.EqualTo("x86_64"));
        Assert.That(native.GetOs(), Is.EqualTo("linux"));
        Assert.That(native.HasVersion(), Is.True);
        Assert.That(native.GetVersion(), Is.EqualTo("musl"));
        Assert.That(native.MatchesText("x86_64-linux-musl"), Is.True);

        Assert.That(
            delegate
            {
                new RubyGemsPlatform("bad platform");
            },
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void RequirementList_CombinesRequirementRangesAndDefensivelyCopiesText()
    {
        string[] requirementTexts = new string[] { ">= 1.0", "< 2.0", "!= 1.5" };
        RubyGemsRequirementList requirements = new RubyGemsRequirementList(requirementTexts);
        requirementTexts[0] = ">= 9.0";
        string[] returnedTexts = requirements.GetRequirementTexts();
        returnedTexts[1] = "< 1.0";

        Assert.That(requirements.GetRequirementCount(), Is.EqualTo(3));
        Assert.That(requirements.GetRequirementText(0), Is.EqualTo(">= 1.0"));
        Assert.That(requirements.ToRequirementText(), Is.EqualTo(">= 1.0, < 2.0, != 1.5"));
        Assert.That(requirements.AllowsVersionText("1.4"), Is.True);
        Assert.That(requirements.AllowsVersionText("1.5"), Is.False);
        Assert.That(requirements.AllowsVersionText("2.0"), Is.False);
        Assert.That(requirements.HasPrereleaseRequirement(), Is.False);
    }

    [Test]
    public void RequirementList_DefaultAllowsAnyRubyGemsVersion()
    {
        RubyGemsRequirementList requirements = RubyGemsVersions.CreateDefaultRequirement();

        Assert.That(requirements.GetRequirementText(0), Is.EqualTo(">= 0"));
        Assert.That(requirements.AllowsVersionText("0"), Is.True);
        Assert.That(requirements.AllowsVersionText("99.0.0"), Is.True);
    }

    [Test]
    public void RequirementList_DetectsPrereleaseRequirements()
    {
        RubyGemsRequirementList requirements = RubyGemsRequirementList.Create(">= 1.0.a");

        Assert.That(requirements.HasPrereleaseRequirement(), Is.True);
        Assert.That(requirements.AllowsVersionText("1.0.a"), Is.True);
        Assert.That(requirements.AllowsVersionText("1.0"), Is.True);
    }

    [Test]
    public void Dependency_StoresNameRequirementAndType()
    {
        RubyGemsDependency runtime = RubyGemsDependency.CreateRuntime("rack", "~> 2.2");
        RubyGemsDependency development = RubyGemsDependency.CreateDevelopment("rspec-core", ">= 3.0");
        RubyGemsSpecification rack = RubyGemsSpecification.Create(
            "rack",
            "2.2.4",
            RubyGemsPlatform.CreateRuby(),
            new RubyGemsDependency[0],
            RubyGemsSource.Create("rubygems", "https://rubygems.org"));

        Assert.That(runtime.GetName(), Is.EqualTo("rack"));
        Assert.That(runtime.IsRuntime(), Is.True);
        Assert.That(runtime.AllowsVersionText("2.2.9"), Is.True);
        Assert.That(runtime.AllowsVersionText("3.0.0"), Is.False);
        Assert.That(runtime.AllowsSpecification(rack), Is.True);
        Assert.That(development.IsDevelopment(), Is.True);
        Assert.That(development.GetDependencyType(), Is.EqualTo(RubyGemsDependencyType.Development));
    }

    [Test]
    public void Specification_StoresIdentityPlatformDependenciesAndSource()
    {
        RubyGemsDependency dependency = RubyGemsDependency.CreateRuntime("rack", ">= 2.0");
        RubyGemsDependency[] dependencies = new RubyGemsDependency[] { dependency };
        RubyGemsSource source = RubyGemsSource.Create("rubygems", "https://rubygems.org");
        RubyGemsSpecification rubySpec = RubyGemsSpecification.Create(
            "rails",
            "7.1.0",
            RubyGemsPlatform.CreateRuby(),
            dependencies,
            source);
        RubyGemsSpecification nativeSpec = RubyGemsSpecification.Create(
            "native",
            "1.0.0",
            new RubyGemsPlatform("x86_64-linux"),
            new RubyGemsDependency[0],
            source);

        dependencies[0] = RubyGemsDependency.CreateRuntime("changed", ">= 0");
        RubyGemsDependency[] returnedDependencies = rubySpec.GetDependencies();
        returnedDependencies[0] = RubyGemsDependency.CreateRuntime("other", ">= 0");

        Assert.That(rubySpec.GetName(), Is.EqualTo("rails"));
        Assert.That(rubySpec.GetVersionText(), Is.EqualTo("7.1.0"));
        Assert.That(rubySpec.GetPlatform().IsRuby(), Is.True);
        Assert.That(rubySpec.GetSource().MatchesName("rubygems"), Is.True);
        Assert.That(rubySpec.GetDependency(0).GetName(), Is.EqualTo("rack"));
        Assert.That(rubySpec.GetFullName(), Is.EqualTo("rails-7.1.0"));
        Assert.That(nativeSpec.GetFullName(), Is.EqualTo("native-1.0.0-x86_64-linux"));
        Assert.That(rubySpec.HasSameIdentity(RubyGemsSpecification.Create("rails", "7.1", RubyGemsPlatform.CreateRuby(), new RubyGemsDependency[0], source)), Is.True);
    }

    [Test]
    public void SourceAndResolverOptions_ExposeConfiguredValuesAndDefensivelyCopyArrays()
    {
        RubyGemsSource rubygems = RubyGemsSource.Create("rubygems", "https://rubygems.org");
        RubyGemsSource mirror = RubyGemsSource.Create("mirror", "https://example.test");
        RubyGemsPlatform ruby = RubyGemsPlatform.CreateRuby();
        RubyGemsPlatform java = RubyGemsPlatform.CreateUniversal("java");
        RubyGemsSource[] sources = new RubyGemsSource[] { rubygems };
        RubyGemsPlatform[] platforms = new RubyGemsPlatform[] { ruby };
        RubyGemsResolverOptions options = RubyGemsResolverOptions.CreateDefault();

        options.SetPrereleasePolicy(PrereleasePolicy.Include);
        options.SetIncludeDevelopmentDependencies(true);
        options.SetMaxBacktrackingSteps(25);
        options.SetSources(sources);
        options.SetPlatforms(platforms);
        sources[0] = mirror;
        platforms[0] = java;
        RubyGemsSource[] returnedSources = options.GetSources();
        RubyGemsPlatform[] returnedPlatforms = options.GetPlatforms();
        returnedSources[0] = mirror;
        returnedPlatforms[0] = java;

        Assert.That(rubygems.GetUri(), Is.EqualTo("https://rubygems.org"));
        Assert.That(options.GetPrereleasePolicy(), Is.EqualTo(PrereleasePolicy.Include));
        Assert.That(options.IncludesDevelopmentDependencies(), Is.True);
        Assert.That(options.GetMaxBacktrackingSteps(), Is.EqualTo(25));
        Assert.That(options.GetSource(0).GetName(), Is.EqualTo("rubygems"));
        Assert.That(options.GetPlatform(0).IsRuby(), Is.True);
    }

    [Test]
    public void Constructors_RejectInvalidInputs()
    {
        Assert.That(
            delegate
            {
                RubyGemsDependency.CreateRuntime("-bad", ">= 0");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                RubyGemsRequirementList.Create(">= bad");
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                new RubyGemsDependency(
                    "rack",
                    RubyGemsRequirementList.Create(">= 0"),
                    (RubyGemsDependencyType)99);
            },
            Throws.TypeOf<ArgumentOutOfRangeException>());

        Assert.That(
            delegate
            {
                RubyGemsSpecification.Create(
                    "rack",
                    "1.0",
                    RubyGemsPlatform.CreateRuby(),
                    new RubyGemsDependency[] { null! },
                    RubyGemsSource.Create("rubygems", "https://rubygems.org"));
            },
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            delegate
            {
                RubyGemsResolverOptions options = RubyGemsResolverOptions.CreateDefault();
                options.SetMaxBacktrackingSteps(0);
            },
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
