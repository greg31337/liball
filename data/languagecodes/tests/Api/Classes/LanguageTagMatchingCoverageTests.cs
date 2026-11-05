using languagecodes;

namespace languagecodes.Tests;

public sealed class LanguageTagMatchingCoverageTests
{
    [Test]
    public void Rfc4647BasicFilteringUsesExactSubtagPrefixes()
    {
        LanguageCodeService service = new LanguageCodeService();
        string[] tags =
        {
            "de-DE",
            "de-DE-1996",
            "de-Deva",
            "de-Latn-DE",
            "de-de-x-goethe"
        };

        LanguageTagMatchResult[] results = service.FilterTags("de-de", tags);

        Assert.Multiple(() =>
        {
            Assert.That(results.Select(result => result.MatchedTag), Is.EqualTo(new[]
            {
                "de-DE",
                "de-DE-1996",
                "de-de-x-goethe"
            }));
            Assert.That(results.Select(result => result.MatchedIndex), Is.EqualTo(
                new[] { 0, 1, 4 }));
            Assert.That(results.All(result =>
                result.Status == LanguageTagMatchStatus.Matched), Is.True);
            Assert.That(results.All(result =>
                result.NormalizedRange == "de-de"), Is.True);
        });
    }

    [Test]
    public void WildcardFilteringMatchesEveryWellFormedCandidateInCallerOrder()
    {
        LanguageCodeService service = new LanguageCodeService();
        string[] tags =
        {
            "fr",
            "X-Company-Internal",
            "i-klingon",
            "abcd-1234"
        };

        LanguageTagMatchResult[] results = service.FilterTags("*", tags);
        LanguageTagMatchResult lookup = service.LookupBestTag("*", tags);

        Assert.Multiple(() =>
        {
            Assert.That(results.Select(result => result.MatchedTag), Is.EqualTo(tags));
            Assert.That(results.Select(result => result.MatchedIndex), Is.EqualTo(
                new[] { 0, 1, 2, 3 }));
            Assert.That(results.All(result => result.NormalizedRange == "*"), Is.True);
            Assert.That(results[1].ParsedTag?.IsPrivateUseOnly, Is.True);
            Assert.That(results[2].ParsedTag?.IsGrandfathered, Is.True);
            Assert.That(lookup.Status, Is.EqualTo(LanguageTagMatchStatus.NoMatch));
            Assert.That(lookup.MatchedTag, Is.Null);
            Assert.That(lookup.MatchedIndex, Is.EqualTo(-1));
            Assert.That(lookup.NormalizedRange, Is.EqualTo("*"));
        });
    }

    [Test]
    public void Rfc4647LookupUsesTheMostSpecificAvailableTruncation()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageTagMatchResult result = service.LookupBestTag(
            "zh-Hant-CN-x-private1-private2",
            new[]
            {
                "zh-Hant-CN",
                "zh-Hant-CN-x-private1",
                "zh-Hant",
                "zh"
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageTagMatchStatus.Matched));
            Assert.That(result.MatchedTag, Is.EqualTo("zh-Hant-CN-x-private1"));
            Assert.That(result.MatchedIndex, Is.EqualTo(1));
            Assert.That(result.NormalizedRange, Is.EqualTo(
                "zh-hant-cn-x-private1-private2"));
            Assert.That(result.ParsedTag?.NormalizedValue, Is.EqualTo(
                "zh-Hant-CN-x-private1"));
        });
    }

    [Test]
    public void LookupTruncationRemovesExtensionSingletonsWithTheirPayloads()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageTagMatchResult extension = service.LookupBestTag(
            "de-DE-a-foo",
            new[] { "de", "de-DE", "de-DE-1996" });
        LanguageTagMatchResult privateUse = service.LookupBestTag(
            "en-US-x-private",
            new[] { "en", "en-US", "en-GB" });

        Assert.Multiple(() =>
        {
            Assert.That(extension.Status, Is.EqualTo(LanguageTagMatchStatus.Matched));
            Assert.That(extension.MatchedTag, Is.EqualTo("de-DE"));
            Assert.That(extension.MatchedIndex, Is.EqualTo(1));
            Assert.That(privateUse.Status, Is.EqualTo(LanguageTagMatchStatus.Matched));
            Assert.That(privateUse.MatchedTag, Is.EqualTo("en-US"));
            Assert.That(privateUse.MatchedIndex, Is.EqualTo(1));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("-en")]
    [TestCase("en-")]
    [TestCase("1n")]
    [TestCase("en_US")]
    [TestCase("en-abcdefghi")]
    [TestCase("en-*")]
    [TestCase("\u00e9n")]
    public void InvalidRangesReturnTypedDiagnostics(string? range)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageTagMatchResult[] filtered = service.FilterTags(
            range,
            new[] { "en", "en-US", "fr" });
        LanguageTagMatchResult lookup = service.LookupBestTag(
            range,
            new[] { "en", "en-US", "fr" });

        Assert.Multiple(() =>
        {
            Assert.That(filtered, Has.Length.EqualTo(1));
            Assert.That(filtered[0].Status, Is.EqualTo(
                LanguageTagMatchStatus.InvalidRange));
            Assert.That(filtered[0].MatchedTag, Is.Null);
            Assert.That(filtered[0].MatchedIndex, Is.EqualTo(-1));
            Assert.That(filtered[0].NormalizedRange, Is.Null);
            Assert.That(lookup.Status, Is.EqualTo(
                LanguageTagMatchStatus.InvalidRange));
            Assert.That(lookup.MatchedTag, Is.Null);
            Assert.That(lookup.MatchedIndex, Is.EqualTo(-1));
            Assert.That(lookup.NormalizedRange, Is.Null);
        });
    }

    [Test]
    public void FilteringAndLookupReportNoMatchWithoutInventingFallbacks()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageTagMatchResult[] filtered = service.FilterTags(
            "fr-CA",
            new[] { "fr", "fr-FR", "en-CA" });
        LanguageTagMatchResult lookup = service.LookupBestTag(
            "fr-CA",
            new[] { "en", "fr-FR", "en-CA" });

        Assert.Multiple(() =>
        {
            Assert.That(filtered, Is.Empty);
            Assert.That(lookup.Status, Is.EqualTo(LanguageTagMatchStatus.NoMatch));
            Assert.That(lookup.MatchedTag, Is.Null);
            Assert.That(lookup.MatchedIndex, Is.EqualTo(-1));
            Assert.That(lookup.NormalizedRange, Is.EqualTo("fr-ca"));
            Assert.That(lookup.ParsedTag, Is.Null);
        });
    }

    [Test]
    public void CallerOrderingAndDuplicateInputsArePreserved()
    {
        LanguageCodeService service = new LanguageCodeService();
        string[] tags =
        {
            "EN-us",
            "fr",
            "en-US",
            "en-US",
            "en-US-x-private",
            "de"
        };

        LanguageTagMatchResult[] filtered = service.FilterTags("en-us", tags);
        LanguageTagMatchResult lookup = service.LookupBestTag("en-US", tags);

        Assert.Multiple(() =>
        {
            Assert.That(filtered.Select(result => result.MatchedTag), Is.EqualTo(new[]
            {
                "EN-us",
                "en-US",
                "en-US",
                "en-US-x-private"
            }));
            Assert.That(filtered.Select(result => result.MatchedIndex), Is.EqualTo(
                new[] { 0, 2, 3, 4 }));
            Assert.That(filtered.Select(result => result.Status), Is.All.EqualTo(
                LanguageTagMatchStatus.Matched));
            Assert.That(lookup.Status, Is.EqualTo(LanguageTagMatchStatus.Matched));
            Assert.That(lookup.MatchedTag, Is.EqualTo("EN-us"));
            Assert.That(lookup.MatchedIndex, Is.Zero);
        });
    }

    [Test]
    public void InvalidCandidatesRetainTheirOriginalPositions()
    {
        LanguageCodeService service = new LanguageCodeService();
        string[] tags =
        {
            "en-US",
            "en--GB",
            null!,
            "sl-rozaj-ROZAJ",
            "en-Latn"
        };

        LanguageTagMatchResult[] results = service.FilterTags("en", tags);

        Assert.Multiple(() =>
        {
            Assert.That(results.Select(result => result.Status), Is.EqualTo(new[]
            {
                LanguageTagMatchStatus.Matched,
                LanguageTagMatchStatus.InvalidTag,
                LanguageTagMatchStatus.InvalidTag,
                LanguageTagMatchStatus.InvalidTag,
                LanguageTagMatchStatus.Matched
            }));
            Assert.That(results.Select(result => result.MatchedIndex), Is.EqualTo(
                new[] { 0, 1, 2, 3, 4 }));
            Assert.That(results[0].MatchedTag, Is.EqualTo("en-US"));
            Assert.That(results[1].MatchedTag, Is.EqualTo("en--GB"));
            Assert.That(results[2].MatchedTag, Is.Null);
            Assert.That(results[3].MatchedTag, Is.EqualTo("sl-rozaj-ROZAJ"));
            Assert.That(results[4].MatchedTag, Is.EqualTo("en-Latn"));
        });
    }
}
