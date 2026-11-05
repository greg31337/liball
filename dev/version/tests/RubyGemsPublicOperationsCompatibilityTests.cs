namespace version.Tests;

[TestFixture]
public class RubyGemsPublicOperationsCompatibilityTests
{
    [Test]
    public void VersionComparison_OrdersAlphanumericGemVersionSegments()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();
        string[] versions = new string[]
        {
            "0.9",
            "1.0.a",
            "1.0.a.1",
            "1.0.a.2",
            "1.0.a.10",
            "1.0.b1",
            "1.0.rc.1",
            "1.0",
            "1.0.0.1"
        };

        int leftIndex = 0;
        while (leftIndex < versions.Length)
        {
            int rightIndex = leftIndex + 1;
            while (rightIndex < versions.Length)
            {
                Assert.That(
                    operations.CompareVersions(versions[leftIndex], versions[rightIndex]),
                    Is.LessThan(0),
                    versions[leftIndex] + " should precede " + versions[rightIndex]);
                Assert.That(
                    operations.CompareVersions(versions[rightIndex], versions[leftIndex]),
                    Is.GreaterThan(0),
                    versions[rightIndex] + " should follow " + versions[leftIndex]);
                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }
    }

    [Test]
    public void VersionComparison_TreatsTrailingZerosAndSplitSegmentsAsEquivalent()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();

        AssertEquivalent(operations, "1", "1.0");
        AssertEquivalent(operations, "1.0", "1.0.0.0");
        AssertEquivalent(operations, "1.0.a", "1.a");
        AssertEquivalent(operations, "1.0.a.0", "1.0.a");
        AssertEquivalent(operations, "1.0.a10", "1.0.a.10");
        Assert.That(operations.NormalizeVersion("001.002b003"), Is.EqualTo("1.2.b.3"));
        Assert.That(operations.NormalizeVersion("1.0.0.alpha.0005"), Is.EqualTo("1.0.0.alpha.5"));
    }

    [Test]
    public void Requirements_ApplyExactDefaultAndPrereleaseDefaults()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();
        RubyGemsRequirementList exact = operations.CreateRequirementList(
            new string?[] { "1.0" });
        RubyGemsRequirementList defaultRequirement = operations.CreateDefaultRequirement();
        RubyGemsRequirementList defaultPrerelease = operations.CreateDefaultPrereleaseRequirement();

        Assert.That(exact.normalizedRequirementTexts, Is.EqualTo(new string[] { "= 1.0" }));
        Assert.That(operations.RequirementAllowsVersion(exact, "1"), Is.True);
        Assert.That(operations.RequirementAllowsVersion(exact, "1.0.0"), Is.True);
        Assert.That(operations.RequirementAllowsVersion(exact, "1.0.1"), Is.False);
        Assert.That(defaultRequirement.normalizedRequirementTexts, Is.EqualTo(new string[] { ">= 0" }));
        Assert.That(operations.RequirementAllowsVersion(defaultRequirement, "5.0"), Is.True);
        Assert.That(operations.RequirementAllowsVersion(defaultRequirement, "1.0.pre"), Is.False);
        Assert.That(defaultPrerelease.normalizedRequirementTexts, Is.EqualTo(new string[] { ">= 0.a" }));
        Assert.That(operations.RequirementAllowsVersion(defaultPrerelease, "1.0.pre"), Is.True);
    }

    [Test]
    public void Requirements_ApplyPessimisticBoundsAndExclusions()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();

        AssertMatches(
            operations,
            "~> 2.2",
            new string[] { "2.2", "2.9.9" },
            new string[] { "2.1.9", "3.0" });
        AssertMatches(
            operations,
            "~> 2.2.0",
            new string[] { "2.2.0", "2.2.99" },
            new string[] { "2.1.9", "2.3.0" });
        AssertMatches(
            operations,
            "~> 2.0.0.a",
            new string[] { "2.0.0.a", "2.0.0.b", "2.0.0", "2.0.9" },
            new string[] { "1.9.9", "2.1.0" });
        AssertMatches(
            operations,
            "~> 2.2, != 2.2.5",
            new string[] { "2.2.4", "2.2.6", "2.9.9" },
            new string[] { "2.1.9", "2.2.5", "3.0" });
    }

    [Test]
    public void Requirements_FlattenNestedNullableAndCommaSeparatedInput()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();
        RubyGemsRequirementList requirements = operations.CreateNestedRequirementList(
            new string?[]?[]
            {
                new string?[] { ">= 1.0, < 2.0", null },
                null,
                new string?[] { ">=1.0", "!= 1.5", "< 2.0" }
            });

        Assert.That(
            requirements.normalizedRequirementTexts,
            Is.EqualTo(new string[] { ">= 1.0", "< 2.0", "!= 1.5" }));
        Assert.That(operations.RequirementAllowsVersion(requirements, "1.4.9"), Is.True);
        Assert.That(operations.RequirementAllowsVersion(requirements, "1.5"), Is.False);
        Assert.That(operations.RequirementAllowsVersion(requirements, "2.0"), Is.False);
    }

    [Test]
    public void Platforms_FilterCompatibleSpecificationsAndSelectBestNativeMatch()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();
        RubyGemsPlatform ruby = new RubyGemsPlatform("ruby");
        RubyGemsPlatform localLinux = new RubyGemsPlatform("x86_64-linux-gnu");
        RubyGemsSpecification rubySpecification = Specification(
            operations,
            "ffi",
            "1.0.0",
            ruby,
            new RubyGemsDependency[0],
            DefaultSource());
        RubyGemsSpecification universalLinux = Specification(
            operations,
            "ffi",
            "1.0.0",
            new RubyGemsPlatform("universal-linux"),
            new RubyGemsDependency[0],
            DefaultSource());
        RubyGemsSpecification exactLinux = Specification(
            operations,
            "ffi",
            "1.0.0",
            localLinux,
            new RubyGemsDependency[0],
            DefaultSource());
        RubyGemsSpecification windows = Specification(
            operations,
            "ffi",
            "1.0.0",
            new RubyGemsPlatform("x64-mingw-ucrt"),
            new RubyGemsDependency[0],
            DefaultSource());

        RubyGemsSpecification[] matches = operations.FilterCompatibleSpecifications(
            new RubyGemsSpecification[] { rubySpecification, universalLinux, exactLinux, windows },
            new RubyGemsPlatform[] { ruby, localLinux });
        RubyGemsSpecification? selected = operations.SelectBestPlatformSpecification(
            matches,
            new RubyGemsPlatform[] { ruby, localLinux });
        RubyGemsSpecification? unavailable = operations.SelectBestPlatformSpecification(
            new RubyGemsSpecification[] { windows },
            new RubyGemsPlatform[] { localLinux });

        Assert.That(operations.PlatformMatches(ruby, ruby), Is.True);
        Assert.That(operations.PlatformMatches(ruby, localLinux), Is.False);
        Assert.That(operations.PlatformMatches(new RubyGemsPlatform("universal-linux"), localLinux), Is.True);
        Assert.That(operations.PlatformMatches(new RubyGemsPlatform("arm-linux"), new RubyGemsPlatform("armv7-linux")), Is.True);
        Assert.That(matches, Is.EqualTo(new RubyGemsSpecification[] { rubySpecification, universalLinux, exactLinux }));
        Assert.That(selected, Is.SameAs(exactLinux));
        Assert.That(unavailable, Is.Null);
    }

    [Test]
    public void Resolver_UsesConfiguredSourceOrderAndRejectsUnlistedSources()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();
        RubyGemsSource rubygems = DefaultSource();
        RubyGemsSource mirror = new RubyGemsSource("mirror", "https://example.test/mirror");
        RubyGemsSpecification rubygemsSpecification = Specification(
            operations,
            "rack",
            "2.2.8",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[0],
            rubygems);
        RubyGemsSpecification mirrorSpecification = Specification(
            operations,
            "rack",
            "2.2.8",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[0],
            mirror);
        RubyGemsDependency root = Dependency(operations, "rack", "= 2.2.8");
        RubyGemsResolverOptions mirrorFirst = new RubyGemsResolverOptions();
        mirrorFirst.sources = new RubyGemsSource[] { mirror, rubygems };
        RubyGemsResolverOptions mirrorOnly = new RubyGemsResolverOptions();
        mirrorOnly.sources = new RubyGemsSource[] { mirror };

        RubyGemsDependencyResolutionResult ordered = operations.ResolveDependencies(
            new RubyGemsDependency[] { root },
            new RubyGemsSpecification[] { rubygemsSpecification, mirrorSpecification },
            mirrorFirst);
        RubyGemsDependencyResolutionResult rejected = operations.ResolveDependencies(
            new RubyGemsDependency[] { root },
            new RubyGemsSpecification[] { rubygemsSpecification },
            mirrorOnly);

        Assert.That(ordered.conflicts, Is.Empty);
        AssertNode(ordered, "rack", "2.2.8", "mirror");
        Assert.That(rejected.nodes, Is.Empty);
        Assert.That(rejected.conflicts, Has.Length.EqualTo(1));
        Assert.That(rejected.conflicts[0].diagnosticCode, Is.Not.Empty);
    }

    [Test]
    public void Resolver_AppliesVersionSpecificExclusions()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();
        RubyGemsSpecification rack227 = Specification(
            operations,
            "rack",
            "2.2.7",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[0],
            DefaultSource());
        RubyGemsSpecification rack228 = Specification(
            operations,
            "rack",
            "2.2.8",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[0],
            DefaultSource());
        RubyGemsResolverOptions options = new RubyGemsResolverOptions();
        options.exclusions = new RubyGemsDependencyExclusion[]
        {
            new RubyGemsDependencyExclusion(
                "rack",
                operations.CreateRequirementList(new string?[] { "= 2.2.8" }))
        };

        RubyGemsDependencyResolutionResult result = operations.ResolveDependencies(
            new RubyGemsDependency[] { Dependency(operations, "rack", "~> 2.2.0") },
            new RubyGemsSpecification[] { rack227, rack228 },
            options);

        Assert.That(result.conflicts, Is.Empty);
        AssertNode(result, "rack", "2.2.7", "rubygems");
    }

    [Test]
    public void Resolver_BacktracksFromHighestCandidateWhenItsDependencyCannotResolve()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();
        RubyGemsSpecification appOne = Specification(
            operations,
            "app",
            "1.0.0",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[] { Dependency(operations, "helper", "= 1.0.0") },
            DefaultSource());
        RubyGemsSpecification appTwo = Specification(
            operations,
            "app",
            "2.0.0",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[] { Dependency(operations, "helper", "= 2.0.0") },
            DefaultSource());
        RubyGemsSpecification helper = Specification(
            operations,
            "helper",
            "1.0.0",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[0],
            DefaultSource());

        RubyGemsDependencyResolutionResult result = operations.ResolveDependencies(
            new RubyGemsDependency[] { Dependency(operations, "app", ">= 1.0, < 3.0") },
            new RubyGemsSpecification[] { appOne, appTwo, helper },
            new RubyGemsResolverOptions());

        Assert.That(result.conflicts, Is.Empty);
        Assert.That(result.nodes, Has.Length.EqualTo(2));
        AssertNode(result, "app", "1.0.0", "rubygems");
        AssertNode(result, "helper", "1.0.0", "rubygems");
    }

    [Test]
    public void Resolver_ReportsConflictsForIncompatibleTransitiveRequirements()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();
        RubyGemsSpecification packageA = Specification(
            operations,
            "a",
            "1.0.0",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[] { Dependency(operations, "shared", "= 1.0.0") },
            DefaultSource());
        RubyGemsSpecification packageB = Specification(
            operations,
            "b",
            "1.0.0",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[] { Dependency(operations, "shared", "= 2.0.0") },
            DefaultSource());
        RubyGemsSpecification sharedOne = Specification(
            operations,
            "shared",
            "1.0.0",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[0],
            DefaultSource());
        RubyGemsSpecification sharedTwo = Specification(
            operations,
            "shared",
            "2.0.0",
            new RubyGemsPlatform("ruby"),
            new RubyGemsDependency[0],
            DefaultSource());

        RubyGemsDependencyResolutionResult result = operations.ResolveDependencies(
            new RubyGemsDependency[]
            {
                Dependency(operations, "a", "= 1.0.0"),
                Dependency(operations, "b", "= 1.0.0")
            },
            new RubyGemsSpecification[] { packageA, packageB, sharedOne, sharedTwo },
            new RubyGemsResolverOptions());

        Assert.That(result.conflicts, Has.Length.EqualTo(1));
        Assert.That(result.conflicts[0].name, Is.Not.Empty);
        Assert.That(result.conflicts[0].diagnosticCode, Is.Not.Empty);
        Assert.That(result.conflicts[0].message, Does.Contain("constraints").IgnoreCase);
    }

    [Test]
    public void ParseOperations_ReturnDiagnosticsForMalformedVersionsAndRequirements()
    {
        IRubyGemsResolution operations = new RubyGemsOperations();
        string[] malformedVersions = new string[] { "bad", "1..0", "1.0-rc1", "1.0+build", "1." };
        string[] malformedRequirements = new string[]
        {
            "",
            "~",
            "~>",
            ">= bad",
            ">= 1.0 < 2.0",
            ">= 1.0,"
        };

        int index = 0;
        while (index < malformedVersions.Length)
        {
            VersionParseResult result = operations.ParseVersion(malformedVersions[index]);
            Assert.That(result.success, Is.False, malformedVersions[index]);
            Assert.That(result.versionPresent, Is.False, malformedVersions[index]);
            Assert.That(result.dialect, Is.EqualTo(VersionDialect.RubyGems), malformedVersions[index]);
            Assert.That(result.diagnostics, Has.Length.EqualTo(1), malformedVersions[index]);
            Assert.That(result.diagnostics[0].code, Is.Not.Empty, malformedVersions[index]);
            index = index + 1;
        }

        index = 0;
        while (index < malformedRequirements.Length)
        {
            VersionRangeParseResult result = operations.ParseRequirement(malformedRequirements[index]);
            Assert.That(result.success, Is.False, malformedRequirements[index]);
            Assert.That(result.range, Is.Null, malformedRequirements[index]);
            Assert.That(result.syntax, Is.EqualTo(RangeSyntax.RubyGems), malformedRequirements[index]);
            Assert.That(result.diagnostics, Has.Length.EqualTo(1), malformedRequirements[index]);
            Assert.That(result.diagnostics[0].code, Is.Not.Empty, malformedRequirements[index]);
            index = index + 1;
        }

        RubyGemsRequirementList valid = operations.CreateRequirementList(new string?[] { ">= 1.0" });
        Assert.That(operations.RequirementAllowsVersion(valid, "bad"), Is.False);
        Assert.That(
            delegate
            {
                operations.NormalizeVersion("bad");
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    static void AssertEquivalent(
        IRubyGemsResolution operations,
        string leftText,
        string rightText)
    {
        Assert.That(operations.CompareVersions(leftText, rightText), Is.EqualTo(0));
        Assert.That(operations.CompareVersions(rightText, leftText), Is.EqualTo(0));
    }

    static void AssertMatches(
        IRubyGemsResolution operations,
        string requirementText,
        string[] matchingVersions,
        string[] rejectedVersions)
    {
        RubyGemsRequirementList requirements = operations.CreateRequirementList(
            new string?[] { requirementText });
        int index = 0;

        while (index < matchingVersions.Length)
        {
            Assert.That(
                operations.RequirementAllowsVersion(requirements, matchingVersions[index]),
                Is.True,
                requirementText + " should contain " + matchingVersions[index]);
            index = index + 1;
        }

        index = 0;
        while (index < rejectedVersions.Length)
        {
            Assert.That(
                operations.RequirementAllowsVersion(requirements, rejectedVersions[index]),
                Is.False,
                requirementText + " should not contain " + rejectedVersions[index]);
            index = index + 1;
        }
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

    static RubyGemsSpecification Specification(
        IRubyGemsResolution operations,
        string name,
        string versionText,
        RubyGemsPlatform platform,
        RubyGemsDependency[] dependencies,
        RubyGemsSource source)
    {
        VersionParseResult result = operations.ParseVersion(versionText);
        Assert.That(result.success, Is.True, versionText);
        Assert.That(result.version, Is.Not.Null, versionText);

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
