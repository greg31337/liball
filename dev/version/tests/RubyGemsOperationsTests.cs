namespace version.Tests;

[TestFixture]
public class RubyGemsOperationsTests
{
    [Test]
    public void VersionOperations_ParseNormalizeDetectPrereleaseAndCompare()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();

        VersionParseResult parsed = operations.ParseVersion(" 001.002.a10 ");

        Assert.That(parsed.success, Is.True);
        Assert.That(parsed.dialect, Is.EqualTo(VersionDialect.RubyGems));
        Assert.That(parsed.versionPresent, Is.True);
        Assert.That(operations.NormalizeVersion(" 001.002.a10 "), Is.EqualTo("1.2.a.10"));
        Assert.That(operations.IsPrereleaseVersion("1.0.a"), Is.True);
        Assert.That(operations.IsPrereleaseVersion("1.0"), Is.False);
        Assert.That(operations.CompareVersions("1.0", "1.0.0"), Is.EqualTo(0));
        Assert.That(operations.CompareVersions("1.0.a", "1.0"), Is.LessThan(0));
        Assert.That(operations.CompareVersions("1.0.a10", "1.0.a9"), Is.GreaterThan(0));
    }

    [Test]
    public void RequirementOperations_HandleDefaultsOperatorsAndNestedInput()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();

        VersionRangeParseResult parsed = operations.ParseRequirement("~> 2.2, != 2.2.5");
        RubyGemsRequirementList defaultRequirements = operations.CreateDefaultRequirement();
        RubyGemsRequirementList prereleaseRequirements = operations.CreateDefaultPrereleaseRequirement();
        RubyGemsRequirementList flat = operations.CreateRequirementList(
            new string?[] { ">= 1.0, < 2.0", null, "!= 1.5" });
        RubyGemsRequirementList nested = operations.CreateNestedRequirementList(
            new string?[]?[]
            {
                new string?[] { ">= 1.0", null },
                null,
                new string?[] { "< 2.0", "!= 1.5" }
            });

        Assert.That(parsed.success, Is.True);
        Assert.That(parsed.syntax, Is.EqualTo(RangeSyntax.RubyGems));
        Assert.That(defaultRequirements.normalizedRequirementTexts, Is.EqualTo(new string[] { ">= 0" }));
        Assert.That(prereleaseRequirements.normalizedRequirementTexts, Is.EqualTo(new string[] { ">= 0.a" }));
        Assert.That(operations.RequirementAllowsVersion(defaultRequirements, "1.0.a"), Is.False);
        Assert.That(operations.RequirementAllowsVersion(prereleaseRequirements, "1.0.a"), Is.True);
        Assert.That(flat.normalizedRequirementTexts, Is.EqualTo(nested.normalizedRequirementTexts));
        Assert.That(operations.RequirementAllowsVersion(flat, "1.4"), Is.True);
        Assert.That(operations.RequirementAllowsVersion(nested, "1.5"), Is.False);
        Assert.That(operations.RequirementAllowsVersion(nested, "2.0"), Is.False);
    }

    [Test]
    public void RequirementOperations_MatchEveryRubyGemsOperator()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();

        Assert.That(Allows(operations, "= 1.2", "1.2.0"), Is.True);
        Assert.That(Allows(operations, "!= 1.2", "1.2.0"), Is.False);
        Assert.That(Allows(operations, "> 1.2", "1.3"), Is.True);
        Assert.That(Allows(operations, ">= 1.2", "1.2"), Is.True);
        Assert.That(Allows(operations, "< 1.2", "1.1"), Is.True);
        Assert.That(Allows(operations, "<= 1.2", "1.2.0"), Is.True);
        Assert.That(Allows(operations, "~> 2.2", "2.9"), Is.True);
        Assert.That(Allows(operations, "~> 2.2", "3.0"), Is.False);
    }

    [Test]
    public void PlatformOperations_FilterAndPreferExactNativeSpecification()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();
        RubyGemsPlatform ruby = new RubyGemsPlatform("ruby");
        RubyGemsPlatform local = new RubyGemsPlatform("x86_64-linux-gnu");
        RubyGemsSpecification rubySpecification = Specification(
            operations,
            "ffi",
            "1.0.0",
            ruby,
            new RubyGemsDependency[0],
            DefaultSource());
        RubyGemsSpecification universalSpecification = Specification(
            operations,
            "ffi",
            "1.0.0",
            new RubyGemsPlatform("universal-linux"),
            new RubyGemsDependency[0],
            DefaultSource());
        RubyGemsSpecification exactSpecification = Specification(
            operations,
            "ffi",
            "1.0.0",
            local,
            new RubyGemsDependency[0],
            DefaultSource());
        RubyGemsSpecification windowsSpecification = Specification(
            operations,
            "ffi",
            "1.0.0",
            new RubyGemsPlatform("x64-mingw-ucrt"),
            new RubyGemsDependency[0],
            DefaultSource());

        RubyGemsSpecification[] filtered = operations.FilterCompatibleSpecifications(
            new RubyGemsSpecification[]
            {
                rubySpecification,
                universalSpecification,
                exactSpecification,
                windowsSpecification
            },
            new RubyGemsPlatform[] { ruby, local });
        RubyGemsSpecification? selected = operations.SelectBestPlatformSpecification(
            filtered,
            new RubyGemsPlatform[] { ruby, local });

        Assert.That(operations.PlatformMatches(new RubyGemsPlatform("universal-linux"), local), Is.True);
        Assert.That(operations.PlatformMatches(new RubyGemsPlatform("arm-linux"), new RubyGemsPlatform("armv7-linux")), Is.True);
        Assert.That(filtered, Has.Length.EqualTo(3));
        Assert.That(selected, Is.SameAs(exactSpecification));
    }

    [Test]
    public void ResolveDependencies_HonorsSourcePriorityAndBacktracks()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();
        RubyGemsSource rubygems = DefaultSource();
        RubyGemsSource mirror = new RubyGemsSource("mirror", "https://example.test/mirror");
        RubyGemsDependency helperOne = Dependency(operations, "helper", "= 1.0.0");
        RubyGemsDependency helperTwo = Dependency(operations, "helper", "= 2.0.0");
        RubyGemsSpecification appOne = Specification(
            operations,
            "app",
            "1.0.0",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[] { helperOne },
            rubygems);
        RubyGemsSpecification appTwo = Specification(
            operations,
            "app",
            "2.0.0",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[] { helperTwo },
            rubygems);
        RubyGemsSpecification helperFromRubyGems = Specification(
            operations,
            "helper",
            "1.0.0",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[0],
            rubygems);
        RubyGemsSpecification helperFromMirror = Specification(
            operations,
            "helper",
            "1.0.0",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[0],
            mirror);
        RubyGemsResolverOptions options = new RubyGemsResolverOptions();
        options.sources = new RubyGemsSource[] { mirror, rubygems };

        RubyGemsDependencyResolutionResult result = operations.ResolveDependencies(
            new RubyGemsDependency[] { Dependency(operations, "app", ">= 1.0.0, < 3.0.0") },
            new RubyGemsSpecification[]
            {
                appOne,
                appTwo,
                helperFromRubyGems,
                helperFromMirror
            },
            options);

        Assert.That(result.conflicts, Is.Empty);
        Assert.That(result.nodes, Has.Length.EqualTo(2));
        AssertNode(result, "app", "1.0.0", "rubygems");
        AssertNode(result, "helper", "1.0.0", "mirror");
    }

    static RubyGemsDependency Dependency(
        IRubyGemsResolution operations,
        string name,
        string requirementText)
    {
        return new RubyGemsDependency(
            name,
            operations.CreateRequirementList(new string?[] { requirementText }),
            RubyGemsDependencyType.Runtime);
    }

    static bool Allows(
        IRubyGemsResolution operations,
        string requirementText,
        string versionText)
    {
        RubyGemsRequirementList requirements = operations.CreateRequirementList(
            new string?[] { requirementText });
        return operations.RequirementAllowsVersion(requirements, versionText);
    }

    static RubyGemsSpecification Specification(
        IRubyGemsResolution operations,
        string name,
        string versionText,
        RubyGemsPlatform platform,
        RubyGemsDependency[] dependencies,
        RubyGemsSource source)
    {
        VersionParseResult result = operations.ParseVersion(versionText);
        Assert.That(result.success, Is.True);
        Assert.That(result.version, Is.Not.Null);

        return new RubyGemsSpecification(
            name,
            result.version!,
            platform,
            dependencies,
            source);
    }

    static RubyGemsSource DefaultSource()
    {
        return new RubyGemsSource("rubygems", "https://rubygems.org");
    }

    static void AssertNode(
        RubyGemsDependencyResolutionResult result,
        string name,
        string versionText,
        string sourceName)
    {
        int index = 0;
        while (index < result.nodes.Length)
        {
            RubyGemsSpecification specification = result.nodes[index].specification;
            if (specification.name == name)
            {
                Assert.That(specification.version.originalText, Is.EqualTo(versionText));
                Assert.That(specification.source.name, Is.EqualTo(sourceName));
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Expected a resolved node named " + name + ".");
    }
}
