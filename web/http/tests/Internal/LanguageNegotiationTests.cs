using System.Text;

namespace http.Tests;

public class LanguageNegotiationTests
{
    [Test]
    public void BasicLanguageRangesParseQualityWildcardAndSpecificity()
    {
        LanguageRangeParseResult result = FieldValueParser.ParseLanguageRanges(
            " , da, en-GB;q=0.8, *;q=0.1,"u8.ToArray(),
            64,
            4,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.EmptyElements, Is.EqualTo(2));
            Assert.That(result.Values, Has.Count.EqualTo(3));
            Assert.That(result.Values[0].Range, Is.EqualTo("da"));
            Assert.That(result.Values[0].Quality, Is.EqualTo(1000));
            Assert.That(result.Values[0].Specificity, Is.EqualTo(1));
            Assert.That(result.Values[1].Range, Is.EqualTo("en-GB"));
            Assert.That(result.Values[1].Quality, Is.EqualTo(800));
            Assert.That(result.Values[1].Specificity, Is.EqualTo(2));
            Assert.That(result.Values[2].IsWildcard, Is.True);
            Assert.That(result.Values[2].Specificity, Is.Zero);
        });
    }

    [Test]
    public void ExtendedLanguageRangesAllowWildcardSubtags()
    {
        LanguageRangeParseResult result = FieldValueParser.ParseLanguageRanges(
            "de-*-DE, *-CH;q=0.7"u8.ToArray(),
            64,
            4,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Values[0].Range, Is.EqualTo("de-*-DE"));
            Assert.That(result.Values[0].Specificity, Is.EqualTo(2));
            Assert.That(result.Values[1].Range, Is.EqualTo("*-CH"));
            Assert.That(result.Values[1].Specificity, Is.EqualTo(1));
        });
    }

    [Test]
    public void LanguageRangeParsingRejectsInvalidGrammarAndLimits()
    {
        string[] invalid =
        {
            "en-*-US",
            "1n-US",
            "en--US",
            "languagex-US",
            "en;level=1",
            "en;q=0.1234"
        };

        Assert.Multiple(() =>
        {
            foreach (string value in invalid)
            {
                LanguageRangeParseResult result = FieldValueParser.ParseLanguageRanges(
                    Encoding.ASCII.GetBytes(value),
                    64,
                    4,
                    false);
                Assert.That(result.IsMalformed, Is.True, value);
            }

            LanguageRangeParseResult limit = FieldValueParser.ParseLanguageRanges(
                "en, fr"u8.ToArray(),
                64,
                1,
                false);
            Assert.That(limit.IsLimitExceeded, Is.True);
        });
    }

    [Test]
    public void BasicFilteringUsesPrefixMatchingQualityAndSpecificExclusion()
    {
        HttpRequest request = Request("en;q=0.8, en-GB;q=0, *;q=0.5");
        HttpNegotiationCandidate british = Candidate(100, 0, "en-GB");
        HttpNegotiationCandidate american = Candidate(0, 1, "en-US");
        HttpNegotiationCandidate french = Candidate(50, 2, "fr");

        HttpNegotiationResult result = NegotiationMatcher.SelectLanguage(
            request,
            new List<HttpNegotiationCandidate> { british, french, american });

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpNegotiationOutcome.Selected));
            Assert.That(result.Candidate, Is.SameAs(american));
            Assert.That(result.HasLanguageMatch, Is.True);
            Assert.That(result.LanguageQuality, Is.EqualTo(800));
            Assert.That(result.LanguageSpecificity, Is.EqualTo(1));
        });
    }

    [Test]
    public void BasicFilteringDoesNotSkipIntermediateSubtags()
    {
        HttpNegotiationCandidate direct = Candidate(0, 0, "de-DE");
        HttpNegotiationCandidate scripted = Candidate(100, 1, "de-Latn-DE");

        HttpNegotiationResult result = NegotiationMatcher.SelectLanguage(
            Request("de-DE"),
            new List<HttpNegotiationCandidate> { scripted, direct });

        Assert.That(result.Candidate, Is.SameAs(direct));
    }

    [Test]
    public void EqualLanguageQualityUsesClientThenServerOrdering()
    {
        HttpNegotiationCandidate english = Candidate(100, 5, "en");
        HttpNegotiationCandidate french = Candidate(0, 9, "fr");
        HttpNegotiationResult client = NegotiationMatcher.SelectLanguage(
            Request("fr;q=0.8, en;q=0.8"),
            new List<HttpNegotiationCandidate> { english, french });

        HttpNegotiationCandidate later = Candidate(5, 4, "fr");
        HttpNegotiationCandidate earlier = Candidate(5, 1, "fr");
        HttpNegotiationResult server = NegotiationMatcher.SelectLanguage(
            Request("fr"),
            new List<HttpNegotiationCandidate> { later, earlier });

        Assert.Multiple(() =>
        {
            Assert.That(client.Candidate, Is.SameAs(french));
            Assert.That(server.Candidate, Is.SameAs(earlier));
        });
    }

    [Test]
    public void AbsentAndEmptyLanguageFieldsRemainDistinct()
    {
        HttpNegotiationCandidate tagged = Candidate(10, 0, "en");
        HttpNegotiationCandidate preferred = Candidate(20, 1, "fr");
        HttpNegotiationResult absent = NegotiationMatcher.SelectLanguage(
            new HttpRequest(),
            new List<HttpNegotiationCandidate> { tagged, preferred });
        HttpNegotiationResult empty = NegotiationMatcher.SelectLanguage(
            Request(""),
            new List<HttpNegotiationCandidate> { tagged, preferred });
        HttpNegotiationCandidate untagged = Candidate(0, 2);
        HttpNegotiationResult nonlinguistic = NegotiationMatcher.SelectLanguage(
            Request(""),
            new List<HttpNegotiationCandidate> { tagged, untagged });

        Assert.Multiple(() =>
        {
            Assert.That(absent.Outcome, Is.EqualTo(HttpNegotiationOutcome.AbsentDimension));
            Assert.That(absent.Candidate, Is.SameAs(preferred));
            Assert.That(absent.HasLanguageMatch, Is.False);
            Assert.That(empty.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.NoAcceptableRepresentation));
            Assert.That(nonlinguistic.Candidate, Is.SameAs(untagged));
        });
    }

    [Test]
    public void ExtendedFilteringMatchesInterveningSubtagsButStopsAtSingletons()
    {
        HttpRequest request = Request("de-*-DE");
        HttpNegotiationCandidate direct = Candidate(1, 0, "de-DE");
        HttpNegotiationCandidate scripted = Candidate(5, 1, "de-Latn-DE-1996");
        HttpNegotiationCandidate privateUse = Candidate(100, 2, "de-x-DE");
        HttpNegotiationCandidate missingRegion = Candidate(100, 3, "de-Deva");

        HttpNegotiationResult result = NegotiationMatcher.SelectLanguageExtended(
            request,
            new List<HttpNegotiationCandidate>
            {
                privateUse,
                missingRegion,
                direct,
                scripted
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidate, Is.SameAs(scripted));
            Assert.That(result.LanguageQuality, Is.EqualTo(1000));
            Assert.That(result.LanguageSpecificity, Is.EqualTo(2));
        });
    }

    [Test]
    public void ExtendedFirstSubtagWildcardMatchesRequestedRegion()
    {
        HttpNegotiationCandidate german = Candidate(0, 0, "de-CH");
        HttpNegotiationCandidate italian = Candidate(5, 1, "it-IT");

        HttpNegotiationResult result = NegotiationMatcher.SelectLanguageExtended(
            Request("*-CH"),
            new List<HttpNegotiationCandidate> { italian, german });

        Assert.That(result.Candidate, Is.SameAs(german));
    }

    [Test]
    public void LookupTruncatesRangesAndRemovesSingletonExtensions()
    {
        HttpNegotiationCandidate general = Candidate(100, 0, "zh-Hant");
        HttpNegotiationCandidate regional = Candidate(0, 1, "zh-Hant-CN");

        HttpNegotiationResult result = NegotiationMatcher.SelectLanguageLookup(
            Request("zh-Hant-CN-x-private1-private2"),
            new List<HttpNegotiationCandidate> { general, regional });

        Assert.Multiple(() =>
        {
            Assert.That(result.Candidate, Is.SameAs(regional));
            Assert.That(result.LanguageQuality, Is.EqualTo(1000));
            Assert.That(result.LanguageSpecificity, Is.EqualTo(3));
        });
    }

    [Test]
    public void LookupDoesNotSelectTagsMoreSpecificThanTheRange()
    {
        HttpNegotiationCandidate longer = Candidate(100, 0, "de-CH-1996");
        HttpNegotiationCandidate fallback = Candidate(0, 1, "de");

        HttpNegotiationResult result = NegotiationMatcher.SelectLanguageLookup(
            Request("de-CH"),
            new List<HttpNegotiationCandidate> { longer, fallback });

        Assert.That(result.Candidate, Is.SameAs(fallback));
    }

    [Test]
    public void LookupUsesQualityOrderAndWildcardDefaultWithExclusions()
    {
        HttpNegotiationCandidate english = Candidate(100, 0, "en");
        HttpNegotiationCandidate german = Candidate(1, 1, "de");
        HttpNegotiationCandidate french = Candidate(0, 2, "fr");
        HttpNegotiationResult quality = NegotiationMatcher.SelectLanguageLookup(
            Request("de;q=0.5, fr;q=0.9"),
            new List<HttpNegotiationCandidate> { german, french });
        HttpNegotiationResult wildcard = NegotiationMatcher.SelectLanguageLookup(
            Request("fr, en;q=0, *;q=0.4"),
            new List<HttpNegotiationCandidate> { english, german });

        Assert.Multiple(() =>
        {
            Assert.That(quality.Candidate, Is.SameAs(french));
            Assert.That(quality.LanguageQuality, Is.EqualTo(900));
            Assert.That(wildcard.Candidate, Is.SameAs(german));
            Assert.That(wildcard.LanguageQuality, Is.EqualTo(400));
            Assert.That(wildcard.LanguageSpecificity, Is.Zero);
        });
    }

    [Test]
    public void LookupPositiveRangeCanFallbackPastZeroQualityWildcard()
    {
        HttpNegotiationCandidate general = Candidate(0, 0, "zh");

        HttpNegotiationResult result = NegotiationMatcher.SelectLanguageLookup(
            Request("zh-Hant-CN, *;q=0"),
            new List<HttpNegotiationCandidate> { general });

        Assert.That(result.Candidate, Is.SameAs(general));
    }

    [Test]
    public void MalformedPreferencesAndCandidatesAreReported()
    {
        HttpNegotiationCandidate candidate = Candidate(0, 0, "en");
        HttpNegotiationResult malformed = NegotiationMatcher.SelectLanguage(
            Request("en-*, fr"),
            new List<HttpNegotiationCandidate> { candidate });
        HttpNegotiationCandidate invalid = Candidate(0, 0, "bad_tag");
        HttpNegotiationCandidate missing = Candidate(0, 0);
        missing.Representation!.Languages = null!;

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Outcome,
                Is.EqualTo(HttpNegotiationOutcome.MalformedPreference));
            Assert.That(malformed.HasLanguageMatch, Is.True);
            Assert.Throws<HttpProtocolException>(() => NegotiationMatcher.SelectLanguage(
                new HttpRequest(),
                new List<HttpNegotiationCandidate> { invalid }));
            Assert.Throws<HttpProtocolException>(() => NegotiationMatcher.SelectLanguageLookup(
                new HttpRequest(),
                new List<HttpNegotiationCandidate> { missing }));
        });
    }

    static HttpRequest Request(string value)
    {
        HttpRequest request = new HttpRequest();
        HttpHeaderField field = new HttpHeaderField();
        field.Name = HttpHeaderNames.AcceptLanguage;
        field.Value = Encoding.ASCII.GetBytes(value);
        request.Headers.Fields.Add(field);
        return request;
    }

    static HttpNegotiationCandidate Candidate(
        int serverPreference,
        int sourceOrder,
        params string[] languages)
    {
        HttpRepresentation representation = new HttpRepresentation();
        representation.Languages.AddRange(languages);
        HttpNegotiationCandidate candidate = new HttpNegotiationCandidate();
        candidate.Representation = representation;
        candidate.ServerPreference = serverPreference;
        candidate.SourceOrder = sourceOrder;
        return candidate;
    }
}
