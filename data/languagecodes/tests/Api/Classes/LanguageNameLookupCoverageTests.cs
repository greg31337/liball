using languagecodes;

namespace languagecodes.Tests;

public sealed class LanguageNameLookupCoverageTests
{
    [TestCase("Algerian Saharan Arabic", "aao", LanguageNameKind.Reference, "en", Iso639Set.Set3)]
    [TestCase("Arbëreshë Albanian", "aae", LanguageNameKind.Print, "en", Iso639Set.Set3)]
    [TestCase("Albanian, Arbëreshë", "aae", LanguageNameKind.Inverted, "en", Iso639Set.Set3)]
    [TestCase("Flemish", "nld", LanguageNameKind.English, "en", Iso639Set.Set2)]
    [TestCase("flamand", "nld", LanguageNameKind.French, "fr", Iso639Set.Set2)]
    public void LookupNameResolvesEveryIsoNameCategory(
        string name,
        string expectedSet3Code,
        LanguageNameKind expectedKind,
        string expectedLanguage,
        Iso639Set expectedSourceSet)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageLookupResult result = service.LookupName(
            name,
            new LanguageLookupOptions { NameLanguage = expectedLanguage });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo(expectedSet3Code));
            Assert.That(result.Candidates, Has.Length.EqualTo(1));
            Assert.That(result.Candidates[0].MatchedValue, Is.EqualTo(name));
            Assert.That(result.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.ExactName));
            Assert.That(
                result.Entry?.Names.Any(
                    item => item.Text == name && item.Kind == expectedKind &&
                        item.NameLanguage == expectedLanguage && item.SourceSet == expectedSourceSet),
                Is.True);
        });
    }

    [Test]
    public void UnicodeNormalizationMatchesCanonicallyEquivalentNames()
    {
        LanguageCodeService service = new LanguageCodeService();
        string decomposed = "Arbe\u0308reshe\u0308 Albanian";

        LanguageLookupResult strict = service.LookupName(
            decomposed,
            new LanguageLookupOptions());
        LanguageLookupResult normalized = service.LookupName(
            decomposed,
            new LanguageLookupOptions { NormalizeNames = true });

        Assert.Multiple(() =>
        {
            Assert.That(strict.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(normalized.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(normalized.Entry?.Set3Code, Is.EqualTo("aae"));
            Assert.That(normalized.Candidates[0].MatchedValue, Is.EqualTo("Arbëreshë Albanian"));
            Assert.That(normalized.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.NormalizedName));
            Assert.That(normalized.NormalizedQuery, Is.EqualTo("ARBËRESHË ALBANIAN"));
        });
    }

    [Test]
    public void CaseWhitespaceAndPunctuationPoliciesAreIndependent()
    {
        LanguageCodeService service = new LanguageCodeService();
        string decorated = "  albanian---arbëreshë!!  ";

        LanguageLookupResult normalized = service.LookupName(
            decorated,
            new LanguageLookupOptions { NormalizeNames = true });
        LanguageLookupResult strict = service.LookupName(
            decorated,
            new LanguageLookupOptions());
        LanguageLookupResult trimmed = service.LookupName(
            "  English  ",
            new LanguageLookupOptions { TrimWhitespace = true });
        LanguageLookupResult foldedCase = service.LookupName(
            "eNgLiSh",
            new LanguageLookupOptions { NormalizeCase = true });

        Assert.Multiple(() =>
        {
            Assert.That(normalized.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(normalized.Entry?.Set3Code, Is.EqualTo("aae"));
            Assert.That(normalized.Candidates[0].MatchedValue, Is.EqualTo("Albanian, Arbëreshë"));
            Assert.That(normalized.NormalizedQuery, Is.EqualTo("ALBANIAN ARBËRESHË"));
            Assert.That(strict.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(trimmed.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(trimmed.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.ExactName));
            Assert.That(foldedCase.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(foldedCase.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.NormalizedName));
        });
    }

    [Test]
    public void NameLanguageFilterResolvesARealCrossLanguageAmbiguity()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions unrestricted = new LanguageLookupOptions
        {
            NormalizeCase = true
        };
        LanguageLookupOptions english = new LanguageLookupOptions
        {
            NormalizeCase = true,
            NameLanguage = "en"
        };
        LanguageLookupOptions french = new LanguageLookupOptions
        {
            NormalizeCase = true,
            NameLanguage = "FR"
        };

        LanguageLookupResult ambiguous = service.LookupName("MONGOL", unrestricted);
        LanguageLookupResult englishResult = service.LookupName("MONGOL", english);
        LanguageLookupResult frenchResult = service.LookupName("MONGOL", french);

        Assert.Multiple(() =>
        {
            Assert.That(ambiguous.Status, Is.EqualTo(LanguageLookupStatus.Ambiguous));
            Assert.That(ambiguous.Entry, Is.Null);
            Assert.That(
                ambiguous.Candidates.Select(candidate => candidate.Entry?.Set3Code),
                Is.EqualTo(new[] { "mgt", "mon" }));
            Assert.That(englishResult.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(englishResult.Entry?.Set3Code, Is.EqualTo("mgt"));
            Assert.That(frenchResult.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(frenchResult.Entry?.Set3Code, Is.EqualTo("mon"));
        });
    }

    [Test]
    public void SearchModesExpandFromExactToPrefixToContains()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageMatch[] exact = service.SearchNames(
            "English",
            SearchOptions(LanguageSearchMode.Exact, 100));
        LanguageMatch[] prefix = service.SearchNames(
            "English",
            SearchOptions(LanguageSearchMode.Prefix, 100));
        LanguageMatch[] contains = service.SearchNames(
            "English",
            SearchOptions(LanguageSearchMode.Contains, 100));

        Assert.Multiple(() =>
        {
            Assert.That(exact.Select(Identity), Is.EqualTo(new[] { "eng:English" }));
            Assert.That(prefix.Length, Is.GreaterThan(exact.Length));
            Assert.That(contains.Length, Is.GreaterThanOrEqualTo(prefix.Length));
            Assert.That(prefix[0].Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(contains[0].Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(prefix.Select(match => match.Rank), Is.EqualTo(Enumerable.Range(0, prefix.Length)));
            Assert.That(
                contains.Select(match => match.Rank),
                Is.EqualTo(Enumerable.Range(0, contains.Length)));
        });
    }

    [Test]
    public void SearchRankingIsStableAndResultLimitReturnsTheRankedPrefix()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions fullOptions = new LanguageLookupOptions
        {
            SearchMode = LanguageSearchMode.Contains,
            ResultLimit = 20,
            NormalizeCase = true,
            NormalizeNames = true
        };
        LanguageLookupOptions limitedOptions = new LanguageLookupOptions
        {
            SearchMode = LanguageSearchMode.Contains,
            ResultLimit = 4,
            NormalizeCase = true,
            NormalizeNames = true
        };

        LanguageMatch[] first = service.SearchNames("arabic", fullOptions);
        LanguageMatch[] second = service.SearchNames("arabic", fullOptions);
        LanguageMatch[] limited = service.SearchNames("arabic", limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(first.Length, Is.GreaterThan(4));
            Assert.That(first.Select(Identity), Is.EqualTo(second.Select(Identity)));
            Assert.That(first.Select(match => match.Rank), Is.EqualTo(Enumerable.Range(0, first.Length)));
            Assert.That(limited, Has.Length.EqualTo(4));
            Assert.That(limited.Select(Identity), Is.EqualTo(first.Take(4).Select(Identity)));
            Assert.That(limited.Select(match => match.Rank), Is.EqualTo(new[] { 0, 1, 2, 3 }));
        });
    }

    [Test]
    public void SearchAppliesCaseInsensitiveNameLanguageFilters()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions english = SearchOptions(LanguageSearchMode.Exact, 10);
        english.NormalizeCase = true;
        english.NameLanguage = "EN";
        LanguageLookupOptions french = SearchOptions(LanguageSearchMode.Exact, 10);
        french.NormalizeCase = true;
        french.NameLanguage = "fr";

        LanguageMatch[] englishMatches = service.SearchNames("MONGOL", english);
        LanguageMatch[] frenchMatches = service.SearchNames("MONGOL", french);

        Assert.Multiple(() =>
        {
            Assert.That(englishMatches.Select(match => match.Entry?.Set3Code), Is.EqualTo(new[] { "mgt" }));
            Assert.That(frenchMatches.Select(match => match.Entry?.Set3Code), Is.EqualTo(new[] { "mon" }));
            Assert.That(englishMatches[0].MatchedValue, Is.EqualTo("Mongol"));
            Assert.That(frenchMatches[0].MatchedValue, Is.EqualTo("mongol"));
        });
    }

    [Test]
    public void ResolveValueUsesCodeBeforeNameAndRequireValuePreservesAmbiguity()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions normalized = new LanguageLookupOptions
        {
            NormalizeCase = true
        };

        LanguageLookupResult codeFirst = service.ResolveValue("En", normalized);
        LanguageLookupResult nameOnly = service.LookupName("En", normalized);

        Assert.Multiple(() =>
        {
            Assert.That(codeFirst.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(codeFirst.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(codeFirst.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.Alpha2));
            Assert.That(nameOnly.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(nameOnly.Entry?.Set3Code, Is.EqualTo("enc"));
            Assert.Throws<AmbiguousLanguageNameException>(() => service.RequireValue("MONGOL", normalized));
        });
    }

    private static LanguageLookupOptions SearchOptions(LanguageSearchMode mode, int limit)
    {
        return new LanguageLookupOptions
        {
            SearchMode = mode,
            ResultLimit = limit
        };
    }

    private static string Identity(LanguageMatch match)
    {
        return (match.Entry?.Set3Code ?? match.Entry?.Set5Code ?? string.Empty) + ":" + match.MatchedValue;
    }
}
