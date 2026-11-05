using languagecodes;

namespace languagecodes.Tests;

public class LanguageTagMatchingEngineTests
{
    [Test]
    public void LookupSelectsTheMostSpecificAvailableFallback()
    {
        LanguageTagMatchResult result = LanguageLookupEngine.LookupBestTag(
            "zh-Hant-CN-x-private1-private2",
            new[] { "zh", "zh-Hant-CN", "zh-Hant", "fr" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageTagMatchStatus.Matched));
            Assert.That(result.MatchedTag, Is.EqualTo("zh-Hant-CN"));
            Assert.That(result.MatchedIndex, Is.EqualTo(1));
            Assert.That(result.NormalizedRange,
                Is.EqualTo("zh-hant-cn-x-private1-private2"));
            Assert.That(result.ParsedTag?.NormalizedValue, Is.EqualTo("zh-Hant-CN"));
        });
    }

    [Test]
    public void LookupDoesNotSelectATagMoreSpecificThanTheRange()
    {
        LanguageTagMatchResult result = LanguageLookupEngine.LookupBestTag(
            "de-CH",
            new[] { "de-CH-1996", "de", "de-DE" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageTagMatchStatus.Matched));
            Assert.That(result.MatchedTag, Is.EqualTo("de"));
            Assert.That(result.MatchedIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void LookupUsesCallerOrderToBreakEquivalentMatches()
    {
        LanguageTagMatchResult result = LanguageLookupEngine.LookupBestTag(
            "EN-us",
            new[] { "en-US", "EN-us", "en", "fr" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageTagMatchStatus.Matched));
            Assert.That(result.MatchedTag, Is.EqualTo("en-US"));
            Assert.That(result.MatchedIndex, Is.Zero);
            Assert.That(result.NormalizedRange, Is.EqualTo("en-us"));
        });
    }

    [Test]
    public void LookupTruncationRemovesSingletonsWithTheirTrailingSubtags()
    {
        string first = LanguageTagMatchingEngine.TruncateLookupRange(
            "zh-hant-cn-x-private1-private2");
        string second = LanguageTagMatchingEngine.TruncateLookupRange(first);
        string extension = LanguageTagMatchingEngine.TruncateLookupRange(
            "de-de-a-foo");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("zh-hant-cn-x-private1"));
            Assert.That(second, Is.EqualTo("zh-hant-cn"));
            Assert.That(extension, Is.EqualTo("de-de"));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("en-")]
    [TestCase("en_Us")]
    [TestCase("en-*")]
    public void LookupRejectsInvalidBasicRanges(string? range)
    {
        LanguageTagMatchResult result = LanguageLookupEngine.LookupBestTag(
            range,
            new[] { "en", "en-US" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(
                LanguageTagMatchStatus.InvalidRange));
            Assert.That(result.MatchedTag, Is.Null);
            Assert.That(result.MatchedIndex, Is.EqualTo(-1));
            Assert.That(result.NormalizedRange, Is.Null);
            Assert.That(result.ParsedTag, Is.Null);
        });
    }

    [Test]
    public void WildcardLookupReturnsNoMatchWithoutAConfiguredDefault()
    {
        LanguageTagMatchResult result = LanguageLookupEngine.LookupBestTag(
            "*",
            new[] { "en", "fr" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageTagMatchStatus.NoMatch));
            Assert.That(result.MatchedTag, Is.Null);
            Assert.That(result.MatchedIndex, Is.EqualTo(-1));
            Assert.That(result.NormalizedRange, Is.EqualTo("*"));
            Assert.That(result.ParsedTag, Is.Null);
        });
    }

    [Test]
    public void LookupSkipsMalformedCandidates()
    {
        string[] tags = new[] { "en--US", null!, "en-US", "en" };
        LanguageTagMatchResult result = LanguageLookupEngine.LookupBestTag(
            "en-US",
            tags);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageTagMatchStatus.Matched));
            Assert.That(result.MatchedTag, Is.EqualTo("en-US"));
            Assert.That(result.MatchedIndex, Is.EqualTo(2));
        });
    }

    [Test]
    public void ExhaustedLookupReturnsNoMatch()
    {
        LanguageTagMatchResult result = LanguageLookupEngine.LookupBestTag(
            "fr-CA",
            new[] { "en", "fr-FR", "en-CA" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageTagMatchStatus.NoMatch));
            Assert.That(result.MatchedTag, Is.Null);
            Assert.That(result.MatchedIndex, Is.EqualTo(-1));
            Assert.That(result.NormalizedRange, Is.EqualTo("fr-ca"));
            Assert.That(result.ParsedTag, Is.Null);
        });
    }

    [Test]
    public void NullLookupCollectionReturnsAnInvalidTagDiagnostic()
    {
        LanguageTagMatchResult result = LanguageLookupEngine.LookupBestTag(
            "en",
            null!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageTagMatchStatus.InvalidTag));
            Assert.That(result.MatchedIndex, Is.EqualTo(-1));
            Assert.That(result.NormalizedRange, Is.EqualTo("en"));
        });
    }

    [Test]
    public void LookupResultsAreRecursiveDefensiveSnapshots()
    {
        string[] tags = new[] { "en-US" };
        LanguageTagMatchResult first = LanguageLookupEngine.LookupBestTag("en-US", tags);
        LanguageTagMatchResult second = LanguageLookupEngine.LookupBestTag("en-US", tags);
        LanguageTag firstParsed = first.ParsedTag!;

        first.MatchedTag = "changed";
        firstParsed.Language = "changed";
        firstParsed.Extlangs = new[] { "changed" };

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.ParsedTag, Is.Not.SameAs(second.ParsedTag));
            Assert.That(second.MatchedTag, Is.EqualTo("en-US"));
            Assert.That(second.ParsedTag?.Language, Is.EqualTo("en"));
            Assert.That(second.ParsedTag?.Extlangs, Is.Empty);
        });
    }

    [Test]
    public void BasicFilteringUsesExactSubtagPrefixesFromRfc4647()
    {
        string[] tags = new[]
        {
            "de-DE",
            "de-DE-1996",
            "de-Deva",
            "de-Latn-DE",
            "de-de-x-goethe"
        };
        LanguageTagMatchResult[] results = LanguageLookupEngine.FilterTags("de-de", tags);

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
    public void PrefixMatchingRequiresAHyphenBoundary()
    {
        LanguageTagMatchResult[] results = LanguageLookupEngine.FilterTags(
            "de",
            new[] { "de", "de-DE", "den", "deu", "deva" });

        Assert.Multiple(() =>
        {
            Assert.That(results.Select(result => result.MatchedTag), Is.EqualTo(
                new[] { "de", "de-DE" }));
            Assert.That(results.Select(result => result.MatchedIndex), Is.EqualTo(
                new[] { 0, 1 }));
        });
    }

    [Test]
    public void WildcardMatchesEveryWellFormedCandidateInInputOrder()
    {
        string[] tags = new[]
        {
            "fr",
            "X-Company-Internal",
            "i-klingon",
            "abcd-1234"
        };
        LanguageTagMatchResult[] results = LanguageLookupEngine.FilterTags("*", tags);

        Assert.Multiple(() =>
        {
            Assert.That(results.Select(result => result.MatchedTag), Is.EqualTo(tags));
            Assert.That(results.Select(result => result.MatchedIndex), Is.EqualTo(
                new[] { 0, 1, 2, 3 }));
            Assert.That(results.All(result => result.NormalizedRange == "*"), Is.True);
            Assert.That(results[1].ParsedTag?.IsPrivateUseOnly, Is.True);
            Assert.That(results[2].ParsedTag?.IsGrandfathered, Is.True);
        });
    }

    [Test]
    public void ComparisonIsCaseInsensitiveWhileReturningOriginalTags()
    {
        LanguageTagMatchResult[] results = LanguageLookupEngine.FilterTags(
            "ZH-hAnS",
            new[] { "zh-Hans", "ZH-HANS-cn", "zh-Hant", "fr" });

        Assert.Multiple(() =>
        {
            Assert.That(results.Select(result => result.MatchedTag), Is.EqualTo(
                new[] { "zh-Hans", "ZH-HANS-cn" }));
            Assert.That(results.Select(result => result.NormalizedRange).Distinct(),
                Is.EqualTo(new[] { "zh-hans" }));
            Assert.That(results[1].ParsedTag?.NormalizedValue, Is.EqualTo("zh-Hans-CN"));
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
    public void InvalidBasicRangesReturnOneDiagnostic(string? range)
    {
        LanguageTagMatchResult[] results = LanguageLookupEngine.FilterTags(
            range,
            new[] { "en", "fr" });

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0].Status, Is.EqualTo(
                LanguageTagMatchStatus.InvalidRange));
            Assert.That(results[0].MatchedTag, Is.Null);
            Assert.That(results[0].MatchedIndex, Is.EqualTo(-1));
            Assert.That(results[0].NormalizedRange, Is.Null);
            Assert.That(results[0].ParsedTag, Is.Null);
        });
    }

    [Test]
    public void BasicRangesDoNotRequireIanaAssignment()
    {
        LanguageTagMatchResult[] results = LanguageLookupEngine.FilterTags(
            "abcd-1234",
            new[] { "abcd-1234", "ABCD-1234-extra", "abcd", "en" });

        Assert.Multiple(() =>
        {
            Assert.That(results.Select(result => result.MatchedTag), Is.EqualTo(
                new[] { "abcd-1234", "ABCD-1234-extra" }));
            Assert.That(results.Select(result => result.MatchedIndex), Is.EqualTo(
                new[] { 0, 1 }));
        });
    }

    [Test]
    public void InvalidCandidatesAreReportedAtTheirOriginalPositions()
    {
        string[] tags = new[]
        {
            "en-US",
            "en--GB",
            null!,
            "sl-rozaj-ROZAJ",
            "en-Latn"
        };
        LanguageTagMatchResult[] results = LanguageLookupEngine.FilterTags("en", tags);

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
            Assert.That(results[1].MatchedTag, Is.EqualTo("en--GB"));
            Assert.That(results[2].MatchedTag, Is.Null);
            Assert.That(results[3].MatchedTag, Is.EqualTo("sl-rozaj-ROZAJ"));
            Assert.That(results[4].ParsedTag?.NormalizedValue, Is.EqualTo("en-Latn"));
        });
    }

    [Test]
    public void NullCandidateCollectionReturnsAnInvalidTagDiagnostic()
    {
        LanguageTagMatchResult[] results = LanguageLookupEngine.FilterTags("en", null!);

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0].Status, Is.EqualTo(LanguageTagMatchStatus.InvalidTag));
            Assert.That(results[0].MatchedIndex, Is.EqualTo(-1));
            Assert.That(results[0].NormalizedRange, Is.EqualTo("en"));
        });
    }

    [Test]
    public void NoMatchesProduceAnEmptyResultSet()
    {
        LanguageTagMatchResult[] results = LanguageLookupEngine.FilterTags(
            "fr-CA",
            new[] { "fr", "fr-FR", "en-CA" });

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void FilteringResultsAreRecursiveDefensiveSnapshots()
    {
        string[] tags = new[] { "en-US" };
        LanguageTagMatchResult[] first = LanguageLookupEngine.FilterTags("en", tags);
        LanguageTagMatchResult[] second = LanguageLookupEngine.FilterTags("en", tags);
        LanguageTag firstParsed = first[0].ParsedTag!;

        first[0].MatchedTag = "changed";
        firstParsed.Language = "changed";
        firstParsed.Extlangs = new[] { "changed" };

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first[0], Is.Not.SameAs(second[0]));
            Assert.That(first[0].ParsedTag, Is.Not.SameAs(second[0].ParsedTag));
            Assert.That(second[0].MatchedTag, Is.EqualTo("en-US"));
            Assert.That(second[0].ParsedTag?.Language, Is.EqualTo("en"));
            Assert.That(second[0].ParsedTag?.Extlangs, Is.Empty);
        });
    }
}
