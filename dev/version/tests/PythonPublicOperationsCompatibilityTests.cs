namespace version.Tests;

[TestFixture]
public class PythonPublicOperationsCompatibilityTests
{
    [Test]
    public void ParsedVersion_ExposesEpochReleasePrePostDevAndLocalSegments()
    {
        IPythonOperations operations = new PythonOperations();
        VersionParseResult result = operations.ParseVersion(
            "2!01.0rc4.post5.dev6+Local-0007");

        Assert.That(result.success, Is.True);
        Assert.That(result.version, Is.Not.Null);
        Assert.That(result.version!.pythonVersion, Is.Not.Null);

        PythonVersion version = result.version.pythonVersion!;
        Assert.That(version.epoch, Is.EqualTo("2"));
        Assert.That(version.releaseSegments, Is.EqualTo(new string[] { "1", "0" }));
        Assert.That(version.preReleasePresent, Is.True);
        Assert.That(version.preReleaseLabel, Is.EqualTo("rc"));
        Assert.That(version.preReleaseNumber, Is.EqualTo("4"));
        Assert.That(version.postReleasePresent, Is.True);
        Assert.That(version.postReleaseNumber, Is.EqualTo("5"));
        Assert.That(version.developmentReleasePresent, Is.True);
        Assert.That(version.developmentReleaseNumber, Is.EqualTo("6"));
        Assert.That(version.localSegments, Has.Length.EqualTo(2));
        Assert.That(version.localSegments[0].text, Is.EqualTo("local"));
        Assert.That(version.localSegments[0].numeric, Is.False);
        Assert.That(version.localSegments[1].text, Is.EqualTo("0007"));
        Assert.That(version.localSegments[1].numeric, Is.True);
    }

    [TestCase("0001!001.002", "1!1.2")]
    [TestCase("0!01.000.5", "1.0.5")]
    [TestCase("1.0alpha1", "1.0a1")]
    [TestCase("1.0beta-2", "1.0b2")]
    [TestCase("1.0PREVIEW_3", "1.0rc3")]
    [TestCase("1.0a", "1.0a0")]
    [TestCase("1.0-1", "1.0.post1")]
    [TestCase("1.0rev0002", "1.0.post2")]
    [TestCase("1.0.post", "1.0.post0")]
    [TestCase("1.0-dev_5", "1.0.dev5")]
    [TestCase("1.0dev", "1.0.dev0")]
    [TestCase("1.0+ABC-001_def002", "1.0+abc.1.def002")]
    public void NormalizeVersion_AcceptsPep440Aliases(string sourceText, string expectedText)
    {
        IPythonOperations operations = new PythonOperations();

        Assert.That(operations.NormalizeVersion(sourceText), Is.EqualTo(expectedText));
    }

    [Test]
    public void CompareVersions_UsesReleasePaddingEpochAndSuffixOrdering()
    {
        IPythonOperations operations = new PythonOperations();
        string[] suffixOrder = new string[]
        {
            "1.0.dev1",
            "1.0a1.dev1",
            "1.0a1",
            "1.0b1",
            "1.0rc1",
            "1.0",
            "1.0.post1.dev1",
            "1.0.post1",
            "1.1.dev1"
        };

        AssertAscending(operations, suffixOrder);
        AssertEquivalent(operations, "1", "1.0.0");
        AssertEquivalent(operations, "1.0", "1.0.0.0");
        Assert.That(operations.CompareVersions("1!1.0", "2014.4"), Is.GreaterThan(0));
        Assert.That(operations.CompareVersions("2!0", "1!999999"), Is.GreaterThan(0));
    }

    [Test]
    public void CompareVersions_OrdersLocalSegmentsUsingPep440Rules()
    {
        IPythonOperations operations = new PythonOperations();

        Assert.That(operations.CompareVersions("1.0", "1.0+abc"), Is.LessThan(0));
        Assert.That(operations.CompareVersions("1.0+abc", "1.0+abc.1"), Is.LessThan(0));
        Assert.That(operations.CompareVersions("1.0+abc", "1.0+abd"), Is.LessThan(0));
        Assert.That(operations.CompareVersions("1.0+abc.1", "1.0+abc.a"), Is.GreaterThan(0));
        AssertEquivalent(operations, "1.0+ABC-001", "1.0+abc.1");
    }

    [Test]
    public void Specifiers_MatchEverySupportedComparisonOperator()
    {
        IPythonOperations operations = new PythonOperations();

        AssertMatches(
            operations,
            "==1.0",
            new string[] { "1", "1.0", "1.0.0" },
            new string[] { "1.0.1", "1.1" });
        AssertMatches(
            operations,
            "!=1.0",
            new string[] { "1.0.1", "1.1" },
            new string[] { "1", "1.0.0" });
        AssertMatches(
            operations,
            ">1.0",
            new string[] { "1.0.1", "2.0" },
            new string[] { "1.0", "1.0rc1" });
        AssertMatches(
            operations,
            ">=1.0",
            new string[] { "1.0", "1.0.1" },
            new string[] { "0.9.9", "1.0rc1" });
        AssertMatches(
            operations,
            "<2.0",
            new string[] { "1.0", "1.9.9" },
            new string[] { "2.0", "2.0.1" });
        AssertMatches(
            operations,
            "<=2.0",
            new string[] { "1.9.9", "2.0" },
            new string[] { "2.0.1" });
        AssertMatches(
            operations,
            "~=1.4.5",
            new string[] { "1.4.5", "1.4.99" },
            new string[] { "1.4.4", "1.5.0" });
    }

    [Test]
    public void Specifiers_MatchWildcardPrefixesAndCommaIntersections()
    {
        IPythonOperations operations = new PythonOperations();

        AssertMatches(
            operations,
            "==1.1.*",
            new string[] { "1.1", "1.1.0", "1.1.9", "1.1.post1" },
            new string[] { "1.0.9", "1.2.0" });
        AssertMatches(
            operations,
            "!=1.1.*",
            new string[] { "1.0.9", "1.2.0" },
            new string[] { "1.1", "1.1.9", "1.1.post1" });
        AssertMatches(
            operations,
            ">=1.0, <2.0, !=1.5",
            new string[] { "1.0", "1.4.9", "1.5.1" },
            new string[] { "0.9.9", "1.5", "2.0" });
    }

    [Test]
    public void PrereleasePolicies_HandleDefaultExplicitIncludeExcludeAndFallback()
    {
        IPythonOperations operations = new PythonOperations();
        VersionRange stableRange = RequireRange(operations, ">=1.0, <2.0");
        VersionRange explicitRange = RequireRange(operations, ">=1.0rc1, <2.0");
        VersionSelectionOptions defaults = new VersionSelectionOptions();
        VersionSelectionOptions include = Options(PrereleasePolicy.Include);
        VersionSelectionOptions exclude = Options(PrereleasePolicy.Exclude);
        VersionSelectionOptions fallback = Options(PrereleasePolicy.AutoIfNoStableCandidate);

        Assert.That(operations.SpecifierAllowsVersion(stableRange, "1.5a1", defaults), Is.False);
        Assert.That(operations.SpecifierAllowsVersion(stableRange, "1.5a1", include), Is.True);
        Assert.That(operations.SpecifierAllowsVersion(explicitRange, "1.5a1", defaults), Is.True);
        Assert.That(operations.SpecifierAllowsVersion(explicitRange, "1.5a1", exclude), Is.False);
        Assert.That(
            operations.FilterCandidates(
                stableRange,
                new string[] { "1.5a1", "1.4" },
                fallback),
            Is.EqualTo(new string[] { "1.4" }));
        Assert.That(
            operations.FilterCandidates(
                stableRange,
                new string[] { "1.5a1", "1.6b1" },
                fallback),
            Is.EqualTo(new string[] { "1.5a1", "1.6b1" }));
    }

    [Test]
    public void CandidateSelection_FiltersInputOrderAndSelectsHighestPep440Version()
    {
        IPythonOperations operations = new PythonOperations();
        VersionRange range = RequireRange(operations, ">=1.0, <2!0");
        VersionSelectionOptions include = Options(PrereleasePolicy.Include);
        string[] candidates = new string[]
        {
            "bad",
            "1.0",
            "v01.005RC1",
            "1.5",
            "1.5+local.2",
            "1!0.1",
            "2!0"
        };

        Assert.That(
            operations.FilterCandidates(range, candidates, include),
            Is.EqualTo(new string[]
            {
                "1.0",
                "v01.005RC1",
                "1.5",
                "1.5+local.2",
                "1!0.1"
            }));
        Assert.That(
            operations.SelectHighestCandidate(range, candidates, include),
            Is.EqualTo("1!0.1"));
        Assert.That(
            operations.SelectHighestCandidate(
                RequireRange(operations, ">=3!0"),
                candidates,
                include),
            Is.Null);
    }

    [Test]
    public void RangeRelations_HandleNestingExclusionsAndDisjointIntervals()
    {
        IPythonOperations operations = new PythonOperations();
        VersionRange broad = RequireRange(operations, ">=1.0, <2.0");
        VersionRange wildcard = RequireRange(operations, "==1.4.*");
        VersionRange excluded = RequireRange(operations, ">=1.0, <2.0, !=1.5");
        VersionRange outside = RequireRange(operations, ">=2.0, <3.0");

        Assert.That(operations.IsSpecifierSubset(wildcard, broad), Is.True);
        Assert.That(operations.IsSpecifierSubset(excluded, broad), Is.True);
        Assert.That(operations.IsSpecifierSubset(broad, excluded), Is.False);
        Assert.That(operations.IsSpecifierSuperset(broad, wildcard), Is.True);
        Assert.That(operations.IsSpecifierSuperset(wildcard, broad), Is.False);
        Assert.That(operations.AreSpecifiersDisjoint(broad, outside), Is.True);
        Assert.That(operations.AreSpecifiersDisjoint(broad, wildcard), Is.False);
    }

    [Test]
    public void CanonicalFormatting_RoundTripsWithoutChangingVersionIdentity()
    {
        IPythonOperations operations = new PythonOperations();
        string[] versions = new string[]
        {
            "1!02.000rc01.post02.dev003+Local-0004",
            "1.0.dev",
            "1.0.post",
            "1.0+abc.01.def002",
            "v1.0-1+Ubuntu_1"
        };
        int index = 0;

        while (index < versions.Length)
        {
            VersionParseResult original = operations.ParseVersion(versions[index]);
            Assert.That(original.success, Is.True, versions[index]);
            Assert.That(original.version, Is.Not.Null, versions[index]);

            string canonical = operations.FormatCanonicalVersion(original.version!);
            VersionParseResult reparsed = operations.ParseVersion(canonical);

            Assert.That(reparsed.success, Is.True, versions[index]);
            Assert.That(reparsed.version, Is.Not.Null, versions[index]);
            Assert.That(operations.CompareVersions(versions[index], canonical), Is.EqualTo(0), versions[index]);
            Assert.That(operations.FormatCanonicalVersion(reparsed.version!), Is.EqualTo(canonical), versions[index]);
            Assert.That(operations.FormatDisplayVersion(original.version!), Is.EqualTo(versions[index]), versions[index]);
            index = index + 1;
        }
    }

    [Test]
    public void InvalidVersionInputs_ReturnStableDiagnostics()
    {
        IPythonOperations operations = new PythonOperations();
        string[] invalidVersions = new string[]
        {
            "",
            "bad",
            "1..0",
            "1.0+",
            "1.0+bad!",
            "1.0..post1"
        };
        int index = 0;

        while (index < invalidVersions.Length)
        {
            VersionParseResult result = operations.ParseVersion(invalidVersions[index]);

            Assert.That(result.success, Is.False, invalidVersions[index]);
            Assert.That(result.versionPresent, Is.False, invalidVersions[index]);
            Assert.That(result.version, Is.Null, invalidVersions[index]);
            Assert.That(result.diagnostics, Has.Length.EqualTo(1), invalidVersions[index]);
            Assert.That(result.diagnostics[0].code, Is.EqualTo(PythonDiagnosticCodes.InvalidVersion), invalidVersions[index]);
            Assert.That(result.diagnostics[0].sourceText, Is.EqualTo(invalidVersions[index]), invalidVersions[index]);
            index = index + 1;
        }
    }

    [Test]
    public void InvalidSpecifierInputs_ReturnStableDiagnostics()
    {
        IPythonOperations operations = new PythonOperations();

        AssertSpecifierFailure(operations, "", PythonSpecifierDiagnosticCodes.MissingSpecifier);
        AssertSpecifierFailure(operations, "1.0", PythonSpecifierDiagnosticCodes.InvalidOperator);
        AssertSpecifierFailure(operations, ">=", PythonSpecifierDiagnosticCodes.MissingVersion);
        AssertSpecifierFailure(operations, ">=bad", PythonSpecifierDiagnosticCodes.InvalidVersion);
        AssertSpecifierFailure(operations, "~=1", PythonSpecifierDiagnosticCodes.InvalidVersion);
        AssertSpecifierFailure(operations, ">1.0.*", PythonSpecifierDiagnosticCodes.InvalidOperator);
        AssertSpecifierFailure(operations, "==1.*.2", PythonSpecifierDiagnosticCodes.InvalidVersion);
        AssertSpecifierFailure(operations, ">=1.0,", PythonSpecifierDiagnosticCodes.MissingSpecifier);
    }

    static void AssertAscending(IPythonOperations operations, string[] versions)
    {
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

    static void AssertEquivalent(
        IPythonOperations operations,
        string leftText,
        string rightText)
    {
        Assert.That(operations.CompareVersions(leftText, rightText), Is.EqualTo(0));
        Assert.That(operations.CompareVersions(rightText, leftText), Is.EqualTo(0));
    }

    static void AssertMatches(
        IPythonOperations operations,
        string specifierText,
        string[] matchingVersions,
        string[] rejectedVersions)
    {
        VersionRange specifier = RequireRange(operations, specifierText);
        VersionSelectionOptions options = new VersionSelectionOptions();
        int index = 0;

        while (index < matchingVersions.Length)
        {
            Assert.That(
                operations.SpecifierAllowsVersion(specifier, matchingVersions[index], options),
                Is.True,
                specifierText + " should contain " + matchingVersions[index]);
            index = index + 1;
        }

        index = 0;
        while (index < rejectedVersions.Length)
        {
            Assert.That(
                operations.SpecifierAllowsVersion(specifier, rejectedVersions[index], options),
                Is.False,
                specifierText + " should not contain " + rejectedVersions[index]);
            index = index + 1;
        }
    }

    static void AssertSpecifierFailure(
        IPythonOperations operations,
        string text,
        string expectedCode)
    {
        VersionRangeParseResult result = operations.ParseSpecifier(text);

        Assert.That(result.success, Is.False, text);
        Assert.That(result.range, Is.Null, text);
        Assert.That(result.syntax, Is.EqualTo(RangeSyntax.Python), text);
        Assert.That(result.diagnostics, Has.Length.EqualTo(1), text);
        Assert.That(result.diagnostics[0].code, Is.EqualTo(expectedCode), text);
        Assert.That(result.diagnostics[0].sourceText, Is.EqualTo(text), text);
    }

    static VersionSelectionOptions Options(PrereleasePolicy policy)
    {
        VersionSelectionOptions options = new VersionSelectionOptions();
        options.prereleasePolicy = policy;
        return options;
    }

    static VersionRange RequireRange(IPythonOperations operations, string text)
    {
        VersionRangeParseResult result = operations.ParseSpecifier(text);
        Assert.That(result.success, Is.True, text);
        Assert.That(result.range, Is.Not.Null, text);
        return result.range!;
    }
}
