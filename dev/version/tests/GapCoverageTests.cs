using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class GapCoverageTests
{
    [Test]
    public void ContainsText_WithParseOptionsHonorsCoerceModeAndBuildMetadataPolicy()
    {
        VersionRange range = Range("1.0.0", "2.0.0");
        VersionParseOptions strict = VersionParseOptions.CreateStrict(VersionDialect.SemVer);
        VersionParseOptions coerce = VersionParseOptions.CreateCoerce(VersionDialect.SemVer);
        VersionParseOptions disallowBuildMetadata = new VersionParseOptions(
            VersionDialect.SemVer,
            VersionParseMode.Strict,
            64,
            true,
            false,
            PrereleasePolicy.Include);

        Assert.That(range.ContainsTextWithOptions("release 1.5", strict), Is.False);
        Assert.That(range.ContainsTextWithOptions("release 1.5", coerce), Is.True);
        Assert.That(range.ContainsTextWithOptions("release 2.0", coerce), Is.False);
        Assert.That(range.ContainsTextWithOptions("1.5.0+build.7", VersionParseOptions.CreateStrict(VersionDialect.SemVer)), Is.True);
        Assert.That(range.ContainsTextWithOptions("1.5.0+build.7", disallowBuildMetadata), Is.False);
    }

    [Test]
    public void RangeInstanceSelectionMethodsHonorSelectionOptions()
    {
        VersionRange range = Range("1.0.0", "3.0.0");
        UniversalVersion[] candidates = Versions(new string[]
        {
            "2.0.0",
            "1.2.0-alpha.1",
            "2.5.0-beta.1",
            "1.5.0"
        });
        VersionSelectionOptions include = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.Include);
        VersionSelectionOptions exclude = VersionSelectionOptions.CreateWithPrereleasePolicy(PrereleasePolicy.Exclude);

        AssertTexts(
            range.FilterCandidatesWithOptions(candidates, include),
            new string[] { "2.0.0", "1.2.0-alpha.1", "2.5.0-beta.1", "1.5.0" });
        AssertTexts(
            range.FilterCandidatesWithOptions(candidates, exclude),
            new string[] { "2.0.0", "1.5.0" });
        Assert.That(range.MinSatisfyingWithOptions(candidates, include)!.GetOriginalText(), Is.EqualTo("1.2.0-alpha.1"));
        Assert.That(range.MaxSatisfyingWithOptions(candidates, include)!.GetOriginalText(), Is.EqualTo("2.5.0-beta.1"));
        Assert.That(range.BestSatisfyingWithOptions(candidates, include)!.GetOriginalText(), Is.EqualTo("2.0.0"));
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
