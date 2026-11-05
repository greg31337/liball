using languagecodes;

namespace languagecodes.Tests;

public class LanguageNameSearchEngineTests
{
    [Test]
    public void ExactSearchDeduplicatesNamesAndReportsLiteralMatchQuality()
    {
        LanguageMatch[] exact = LanguageLookupEngine.SearchNames(
            "English",
            CreateOptions(LanguageSearchMode.Exact, 10, false, false, false, null));
        LanguageMatch[] strictCase = LanguageLookupEngine.SearchNames(
            "english",
            CreateOptions(LanguageSearchMode.Exact, 10, false, false, false, null));
        LanguageMatch[] foldedCase = LanguageLookupEngine.SearchNames(
            "english",
            CreateOptions(LanguageSearchMode.Exact, 10, false, true, false, null));

        Assert.Multiple(() =>
        {
            Assert.That(exact, Has.Length.EqualTo(1));
            Assert.That(exact[0].Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(exact[0].MatchedValue, Is.EqualTo("English"));
            Assert.That(exact[0].Kind, Is.EqualTo(LanguageMatchKind.ExactName));
            Assert.That(exact[0].Rank, Is.Zero);
            Assert.That(strictCase, Is.Empty);
            Assert.That(foldedCase, Has.Length.EqualTo(1));
            Assert.That(foldedCase[0].Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(foldedCase[0].Kind, Is.EqualTo(LanguageMatchKind.NormalizedName));
        });
    }

    [Test]
    public void FullNormalizationAndNameLanguageFilteringAreAppliedToSearch()
    {
        LanguageMatch[] normalized = LanguageLookupEngine.SearchNames(
            "  albanian---arb\u00ebresh\u00eb!! ",
            CreateOptions(LanguageSearchMode.Exact, 10, false, false, true, null));
        LanguageMatch[] french = LanguageLookupEngine.SearchNames(
            "ANGLAIS",
            CreateOptions(LanguageSearchMode.Exact, 10, false, true, false, "FR"));
        LanguageMatch[] excluded = LanguageLookupEngine.SearchNames(
            "ANGLAIS",
            CreateOptions(LanguageSearchMode.Exact, 10, false, true, false, "en"));

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Has.Length.EqualTo(1));
            Assert.That(normalized[0].Entry?.Set3Code, Is.EqualTo("aae"));
            Assert.That(normalized[0].MatchedValue, Is.EqualTo("Albanian, Arb\u00ebresh\u00eb"));
            Assert.That(normalized[0].Kind, Is.EqualTo(LanguageMatchKind.NormalizedName));
            Assert.That(french, Has.Length.EqualTo(1));
            Assert.That(french[0].Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(french[0].MatchedValue, Is.EqualTo("anglais"));
            Assert.That(excluded, Is.Empty);
        });
    }

    [Test]
    public void ContainsSearchRanksExactThenPrefixThenContainsWithStableTies()
    {
        LanguageRow[] languages =
        {
            CreateRow("set3:ccc", "ccc"),
            CreateRow("set3:ddd", "ddd"),
            CreateRow("set3:aaa", "aaa"),
            CreateRow("set3:bbb", "bbb")
        };
        NameRow[] names =
        {
            CreateName("set3:ccc", "Something Alpha", LanguageNameKind.Reference),
            CreateName("set3:ddd", "Alpha Plus", LanguageNameKind.Reference),
            CreateName("set3:aaa", "ALPHA", LanguageNameKind.Print),
            CreateName("set3:bbb", "Alpha Plus", LanguageNameKind.Reference),
            CreateName("set3:aaa", "Alpha", LanguageNameKind.Reference)
        };
        Dictionary<string, NameIndexEntry[]> index = NameIndexBuilder.BuildIndex(languages, names);
        string[] sortedValues = NameIndexBuilder.BuildSortedValues(index);
        LanguageCatalogData catalog = BuildCatalog(languages, names);
        LanguageLookupOptions allOptions = CreateOptions(
            LanguageSearchMode.Contains,
            10,
            false,
            false,
            true,
            null);

        LanguageMatch[] first = LanguageLookupEngine.SearchNameEntries(
            "Alpha",
            allOptions,
            index,
            sortedValues,
            catalog);
        LanguageMatch[] second = LanguageLookupEngine.SearchNameEntries(
            "Alpha",
            allOptions,
            index,
            sortedValues,
            catalog);
        LanguageMatch[] limited = LanguageLookupEngine.SearchNameEntries(
            "Alpha",
            CreateOptions(LanguageSearchMode.Contains, 3, false, false, true, null),
            index,
            sortedValues,
            catalog);

        Assert.Multiple(() =>
        {
            Assert.That(
                first.Select(match => match.Entry?.Set3Code),
                Is.EqualTo(new[] { "aaa", "bbb", "ddd", "ccc" }));
            Assert.That(
                first.Select(match => match.MatchedValue),
                Is.EqualTo(new[] { "Alpha", "Alpha Plus", "Alpha Plus", "Something Alpha" }));
            Assert.That(
                first.Select(match => match.Rank),
                Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(first[0].Kind, Is.EqualTo(LanguageMatchKind.ExactName));
            Assert.That(first.Skip(1).All(match => match.Kind == LanguageMatchKind.NormalizedName), Is.True);
            Assert.That(
                second.Select(match => match.Entry?.Set3Code),
                Is.EqualTo(first.Select(match => match.Entry?.Set3Code)));
            Assert.That(
                limited.Select(match => match.Entry?.Set3Code),
                Is.EqualTo(new[] { "aaa", "bbb", "ddd" }));
        });
    }

    [Test]
    public void SearchModesRestrictMatchesWithoutChangingRankOrder()
    {
        LanguageRow[] languages =
        {
            CreateRow("set3:ccc", "ccc"),
            CreateRow("set3:aaa", "aaa"),
            CreateRow("set3:bbb", "bbb")
        };
        NameRow[] names =
        {
            CreateName("set3:ccc", "Something Alpha", LanguageNameKind.Reference),
            CreateName("set3:aaa", "Alpha", LanguageNameKind.Reference),
            CreateName("set3:bbb", "Alpha Plus", LanguageNameKind.Reference)
        };
        Dictionary<string, NameIndexEntry[]> index = NameIndexBuilder.BuildIndex(languages, names);
        string[] sortedValues = NameIndexBuilder.BuildSortedValues(index);
        LanguageCatalogData catalog = BuildCatalog(languages, names);

        LanguageMatch[] exact = SearchFixture(
            "Alpha",
            LanguageSearchMode.Exact,
            index,
            sortedValues,
            catalog);
        LanguageMatch[] prefix = SearchFixture(
            "Alpha",
            LanguageSearchMode.Prefix,
            index,
            sortedValues,
            catalog);
        LanguageMatch[] contains = SearchFixture(
            "Alpha",
            LanguageSearchMode.Contains,
            index,
            sortedValues,
            catalog);

        Assert.Multiple(() =>
        {
            Assert.That(
                exact.Select(match => match.Entry?.Set3Code),
                Is.EqualTo(new[] { "aaa" }));
            Assert.That(
                prefix.Select(match => match.Entry?.Set3Code),
                Is.EqualTo(new[] { "aaa", "bbb" }));
            Assert.That(
                contains.Select(match => match.Entry?.Set3Code),
                Is.EqualTo(new[] { "aaa", "bbb", "ccc" }));
        });
    }

    [Test]
    public void EmptyInvalidAndNonPositiveLimitedSearchesReturnNoMatches()
    {
        LanguageLookupOptions punctuationOptions = CreateOptions(
            LanguageSearchMode.Contains,
            10,
            false,
            false,
            true,
            null);

        LanguageMatch[] nullQuery = LanguageLookupEngine.SearchNames(null, punctuationOptions);
        LanguageMatch[] empty = LanguageLookupEngine.SearchNames(string.Empty, punctuationOptions);
        LanguageMatch[] punctuation = LanguageLookupEngine.SearchNames("---", punctuationOptions);
        LanguageMatch[] zeroLimit = LanguageLookupEngine.SearchNames(
            "English",
            CreateOptions(LanguageSearchMode.Exact, 0, false, false, false, null));
        LanguageMatch[] negativeLimit = LanguageLookupEngine.SearchNames(
            "English",
            CreateOptions(LanguageSearchMode.Exact, -1, false, false, false, null));
        LanguageMatch[] invalidMode = LanguageLookupEngine.SearchNames(
            "English",
            CreateOptions((LanguageSearchMode)99, 10, false, false, false, null));

        Assert.Multiple(() =>
        {
            Assert.That(nullQuery, Is.Empty);
            Assert.That(empty, Is.Empty);
            Assert.That(punctuation, Is.Empty);
            Assert.That(zeroLimit, Is.Empty);
            Assert.That(negativeLimit, Is.Empty);
            Assert.That(invalidMode, Is.Empty);
        });
    }

    [Test]
    public void SearchResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageLookupOptions options = CreateOptions(
            LanguageSearchMode.Exact,
            10,
            false,
            false,
            false,
            null);
        LanguageMatch[] first = LanguageLookupEngine.SearchNames("English", options);
        LanguageMatch[] second = LanguageLookupEngine.SearchNames("English", options);
        string originalName = second[0].Entry!.Names[0].Text!;

        first[0].MatchedValue = "changed";
        first[0].Entry!.Set3Code = "changed";
        first[0].Entry!.Names[0].Text = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first[0], Is.Not.SameAs(second[0]));
            Assert.That(first[0].Entry, Is.Not.SameAs(second[0].Entry));
            Assert.That(second[0].MatchedValue, Is.EqualTo("English"));
            Assert.That(second[0].Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(second[0].Entry?.Names[0].Text, Is.EqualTo(originalName));
            Assert.That(
                LanguageLookupEngine.SearchNames("English", options)[0].Entry?.Set3Code,
                Is.EqualTo("eng"));
        });
    }

    private static LanguageMatch[] SearchFixture(
        string query,
        LanguageSearchMode mode,
        Dictionary<string, NameIndexEntry[]> index,
        string[] sortedValues,
        LanguageCatalogData catalog)
    {
        return LanguageLookupEngine.SearchNameEntries(
            query,
            CreateOptions(mode, 10, false, false, true, null),
            index,
            sortedValues,
            catalog);
    }

    private static LanguageLookupOptions CreateOptions(
        LanguageSearchMode mode,
        int limit,
        bool trimWhitespace,
        bool normalizeCase,
        bool normalizeNames,
        string? nameLanguage)
    {
        return new LanguageLookupOptions
        {
            SearchMode = mode,
            ResultLimit = limit,
            TrimWhitespace = trimWhitespace,
            NormalizeCase = normalizeCase,
            NormalizeNames = normalizeNames,
            NameLanguage = nameLanguage
        };
    }

    private static LanguageCatalogData BuildCatalog(
        LanguageRow[] languages,
        NameRow[] names)
    {
        return LanguageCatalogBuilder.BuildCatalog(
            languages,
            names,
            Array.Empty<AliasRow>(),
            Array.Empty<RelationshipRow>(),
            Array.Empty<RetirementRow>());
    }

    private static LanguageRow CreateRow(string key, string code)
    {
        return new LanguageRow(
            key,
            null,
            null,
            null,
            code,
            null,
            LanguageScope.Individual,
            LanguageType.Living,
            LanguageCodeStatus.Active,
            ReconciledGroupKind.None,
            null);
    }

    private static NameRow CreateName(
        string entityKey,
        string text,
        LanguageNameKind kind)
    {
        return new NameRow(
            entityKey,
            text,
            "en",
            kind,
            ReconciledSource.Iso639Set3,
            kind == LanguageNameKind.Reference);
    }
}
