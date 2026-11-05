using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionSelectorSelectionTests
{
    [Test]
    public void MinSatisfying_ReturnsLowestFilteredCandidate()
    {
        VersionRange range = Range("1.0.0", "3.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "2.0.0", "1.5.0", "1.0.0", "3.0.0" });

        UniversalVersion? selected = VersionSelector.MinSatisfying(range, candidates);

        Assert.That(selected, Is.Not.Null);
        Assert.That(selected!.GetOriginalText(), Is.EqualTo("1.0.0"));
    }

    [Test]
    public void MaxSatisfying_ReturnsHighestFilteredCandidate()
    {
        VersionRange range = Range("1.0.0", "3.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "2.0.0", "1.5.0", "2.5.0", "3.0.0" });

        UniversalVersion? selected = VersionSelector.MaxSatisfying(range, candidates);

        Assert.That(selected, Is.Not.Null);
        Assert.That(selected!.GetOriginalText(), Is.EqualTo("2.5.0"));
    }

    [Test]
    public void MaxSatisfying_UsesPrereleasePolicyOptions()
    {
        VersionRange range = Range("1.0.0", "3.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "2.5.0", "2.9.0-alpha.1" });
        VersionSelectionOptions include = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.Include);

        UniversalVersion? defaultSelected = VersionSelector.MaxSatisfying(range, candidates);
        UniversalVersion? includeSelected = VersionSelector.MaxSatisfying(range, candidates, include);

        Assert.That(defaultSelected, Is.Not.Null);
        Assert.That(defaultSelected!.GetOriginalText(), Is.EqualTo("2.5.0"));
        Assert.That(includeSelected, Is.Not.Null);
        Assert.That(includeSelected!.GetOriginalText(), Is.EqualTo("2.9.0-alpha.1"));
    }

    [Test]
    public void BestSatisfying_PrefersHighestStableCandidate()
    {
        VersionRange range = Range("1.0.0", "3.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "2.5.0", "2.9.0-alpha.1" });
        VersionSelectionOptions include = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.Include);

        UniversalVersion? selected = VersionSelector.BestSatisfying(range, candidates, include);

        Assert.That(selected, Is.Not.Null);
        Assert.That(selected!.GetOriginalText(), Is.EqualTo("2.5.0"));
    }

    [Test]
    public void BestSatisfying_FallsBackToHighestPrereleaseWhenNoStableMatches()
    {
        VersionRange range = Range("1.0.0", "3.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "2.4.0-alpha.1", "2.5.0-beta.1" });
        VersionSelectionOptions include = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.Include);

        UniversalVersion? selected = VersionSelector.BestSatisfying(range, candidates, include);

        Assert.That(selected, Is.Not.Null);
        Assert.That(selected!.GetOriginalText(), Is.EqualTo("2.5.0-beta.1"));
    }

    [Test]
    public void AutoIfNoStableCandidatePolicyAffectsSelectionBeforeOrdering()
    {
        VersionRange range = Range("1.0.0", "3.0.0");
        UniversalVersion[] withStable = Versions(new string[] { "2.5.0", "2.9.0-alpha.1" });
        UniversalVersion[] onlyPrerelease = Versions(new string[] { "2.4.0-alpha.1", "2.9.0-alpha.1" });
        VersionSelectionOptions options = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.AutoIfNoStableCandidate);

        UniversalVersion? stableSelected = VersionSelector.MaxSatisfying(range, withStable, options);
        UniversalVersion? prereleaseSelected = VersionSelector.MaxSatisfying(range, onlyPrerelease, options);

        Assert.That(stableSelected, Is.Not.Null);
        Assert.That(stableSelected!.GetOriginalText(), Is.EqualTo("2.5.0"));
        Assert.That(prereleaseSelected, Is.Not.Null);
        Assert.That(prereleaseSelected!.GetOriginalText(), Is.EqualTo("2.9.0-alpha.1"));
    }

    [Test]
    public void Selection_ReturnsNullWhenNoCandidateMatches()
    {
        VersionRange range = Range("1.0.0", "2.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "0.9.9", "2.0.0" });

        Assert.That(VersionSelector.MinSatisfying(range, candidates), Is.Null);
        Assert.That(VersionSelector.MaxSatisfying(range, candidates), Is.Null);
        Assert.That(VersionSelector.BestSatisfying(range, candidates), Is.Null);
    }

    [Test]
    public void RangeInstanceSelectionMethodsDelegateToSelector()
    {
        VersionRange range = Range("1.0.0", "3.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "2.0.0", "1.5.0", "2.5.0" });

        Assert.That(range.MinSatisfying(candidates)!.GetOriginalText(), Is.EqualTo("1.5.0"));
        Assert.That(range.MaxSatisfying(candidates)!.GetOriginalText(), Is.EqualTo("2.5.0"));
        Assert.That(range.BestSatisfying(candidates)!.GetOriginalText(), Is.EqualTo("2.5.0"));
    }

    [Test]
    public void Selection_RejectsInvalidArguments()
    {
        VersionRange range = Range("1.0.0", "2.0.0");
        UniversalVersion[] candidates = Versions(new string[] { "1.0.0" });
        VersionSelectionOptions options = VersionSelectionOptions.CreateDefault();

        Assert.That(
            delegate
            {
                VersionSelector.MinSatisfying(null!, candidates, options);
            },
            Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(
            delegate
            {
                VersionSelector.MaxSatisfying(range, null!, options);
            },
            Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(
            delegate
            {
                VersionSelector.BestSatisfying(range, candidates, null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
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
}
