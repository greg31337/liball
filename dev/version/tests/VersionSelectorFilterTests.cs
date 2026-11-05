using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionSelectorFilterTests
{
    [Test]
    public void Filter_DefaultPolicyExcludesPrereleaseCandidates()
    {
        VersionRange range = Range("1.0.0", "2.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "1.0.0", "1.5.0-alpha.1", "1.5.0", "2.0.0" });

        UniversalVersion[] filtered = VersionSelector.Filter(range, candidates);

        AssertTexts(filtered, new string[] { "1.0.0", "1.5.0" });
    }

    [Test]
    public void Filter_IncludePolicyAllowsPrereleaseCandidates()
    {
        VersionRange range = Range("1.0.0", "2.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "1.0.0", "1.5.0-alpha.1", "1.5.0" });
        VersionSelectionOptions options = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.Include);

        UniversalVersion[] filtered = VersionSelector.Filter(range, candidates, options);

        AssertTexts(filtered, new string[] { "1.0.0", "1.5.0-alpha.1", "1.5.0" });
    }

    [Test]
    public void Filter_ExcludePolicyRejectsPrereleaseCandidatesEvenWhenRangeMentionsPrerelease()
    {
        VersionRange range = Range("1.0.0-alpha.1", "2.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "1.0.0-alpha.2", "1.0.0", "1.5.0-alpha.1" });
        VersionSelectionOptions options = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.Exclude);

        UniversalVersion[] filtered = VersionSelector.Filter(range, candidates, options);

        AssertTexts(filtered, new string[] { "1.0.0" });
    }

    [Test]
    public void Filter_ExplicitOnlyPolicyAllowsPrereleasesOnlyWhenComparatorNamesSameCore()
    {
        VersionRange range = Range("1.0.0-alpha.1", "2.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "1.0.0-alpha.2", "1.0.0", "1.5.0-alpha.1", "1.5.0" });
        VersionSelectionOptions options = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.ExplicitOnly);

        UniversalVersion[] filtered = VersionSelector.Filter(range, candidates, options);

        AssertTexts(filtered, new string[] { "1.0.0-alpha.2", "1.0.0", "1.5.0" });
    }

    [Test]
    public void Filter_AutoIfNoStableCandidatePrefersStableMatches()
    {
        VersionRange range = Range("1.0.0", "2.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "1.5.0-alpha.1", "1.5.0", "1.6.0-alpha.1" });
        VersionSelectionOptions options = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.AutoIfNoStableCandidate);

        UniversalVersion[] filtered = VersionSelector.Filter(range, candidates, options);

        AssertTexts(filtered, new string[] { "1.5.0" });
    }

    [Test]
    public void Filter_AutoIfNoStableCandidateAllowsPrereleasesWhenNoStableMatches()
    {
        VersionRange range = Range("1.0.0", "2.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "1.5.0-alpha.1", "1.6.0-alpha.1" });
        VersionSelectionOptions options = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.AutoIfNoStableCandidate);

        UniversalVersion[] filtered = VersionSelector.Filter(range, candidates, options);

        AssertTexts(filtered, new string[] { "1.5.0-alpha.1", "1.6.0-alpha.1" });
    }

    [Test]
    public void Filter_InstanceMethodPreservesCandidateOrder()
    {
        VersionRange range = Range("1.0.0", "3.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "2.0.0", "1.0.0", "3.0.0", "1.5.0" });

        UniversalVersion[] filtered = range.FilterCandidates(candidates);

        AssertTexts(filtered, new string[] { "2.0.0", "1.0.0", "1.5.0" });
    }

    [Test]
    public void Filter_DefaultPolicyUsesRangePrereleaseDefault()
    {
        VersionRangeParseResult result = PythonSpecifierParser.Parse(">=1.0a1, <2.0");
        Assert.That(result.IsSuccess(), Is.True);
        VersionRange range = result.GetRange();
        UniversalVersion[] candidates = new UniversalVersion[]
        {
            ParsePython("1.0a1"),
            ParsePython("1.5a1"),
            ParsePython("1.5")
        };

        UniversalVersion[] filtered = VersionSelector.Filter(range, candidates);

        AssertTexts(filtered, new string[] { "1.0a1", "1.5a1", "1.5" });
    }

    [Test]
    public void Options_ValidatePrereleasePolicy()
    {
        VersionSelectionOptions options = new VersionSelectionOptions();
        options.SetPrereleasePolicy(PrereleasePolicy.Include);

        Assert.That(options.GetPrereleasePolicy(), Is.EqualTo(PrereleasePolicy.Include));
        Assert.That(
            delegate
            {
                options.SetPrereleasePolicy((PrereleasePolicy)99);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void Filter_RejectsInvalidArguments()
    {
        VersionRange range = Range("1.0.0", "2.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "1.0.0" });
        VersionSelectionOptions options = VersionSelectionOptions.CreateDefault();

        Assert.That(
            delegate
            {
                VersionSelector.Filter(null!, candidates, options);
            },
            Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(
            delegate
            {
                VersionSelector.Filter(range, null!, options);
            },
            Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(
            delegate
            {
                VersionSelector.Filter(range, candidates, null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(
            delegate
            {
                VersionSelector.Filter(range, new UniversalVersion[] { null! }, options);
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    static VersionRange Range(string lowerText, string upperText)
    {
        VersionComparatorSet set = new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion(lowerText), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion(upperText), VersionComparisonMode.Precedence)
        });

        return VersionRange.CreateSingleSet(set);
    }

    static UniversalVersion[] Versions(string[] texts)
    {
        UniversalVersion[] versions = new UniversalVersion[texts.Length];
        int index = 0;

        while (index < texts.Length)
        {
            versions[index] = ParseVersion(texts[index]);
            index = index + 1;
        }

        return versions;
    }

    static UniversalVersion ParseVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        return result.GetVersion();
    }

    static UniversalVersion ParsePython(string text)
    {
        VersionParseResult result = PythonVersionParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        return result.GetVersion();
    }

    static void AssertTexts(UniversalVersion[] versions, string[] expectedTexts)
    {
        Assert.That(versions.Length, Is.EqualTo(expectedTexts.Length));
        int index = 0;

        while (index < expectedTexts.Length)
        {
            Assert.That(versions[index].GetOriginalText(), Is.EqualTo(expectedTexts[index]));
            index = index + 1;
        }
    }
}
