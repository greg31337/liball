using languagecodes;

namespace languagecodes.Tests;

public class LanguageNameLookupEngineTests
{
    [Test]
    public void LiteralAndCaseNormalizedLookupReportMatchQuality()
    {
        LanguageLookupResult exact = LanguageLookupEngine.LookupName(
            "English",
            CreateOptions(false, false, false, null));
        LanguageLookupResult foldedCase = LanguageLookupEngine.LookupName(
            "eNgLiSh",
            CreateOptions(false, true, false, "EN"));

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(exact.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(exact.Candidates, Has.Length.EqualTo(1));
            Assert.That(exact.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.ExactName));
            Assert.That(exact.Candidates[0].MatchedValue, Is.EqualTo("English"));
            Assert.That(exact.NormalizedQuery, Is.EqualTo("English"));
            Assert.That(foldedCase.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(foldedCase.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(
                foldedCase.Candidates[0].Kind,
                Is.EqualTo(LanguageMatchKind.NormalizedName));
            Assert.That(foldedCase.NormalizedQuery, Is.EqualTo("ENGLISH"));
        });
    }

    [Test]
    public void UnicodePunctuationAndWhitespaceFoldingRequireNameNormalization()
    {
        string query = "  albanian---arb\u00ebresh\u00eb!! ";
        LanguageLookupResult strict = LanguageLookupEngine.LookupName(
            query,
            CreateOptions(false, false, false, null));
        LanguageLookupResult normalized = LanguageLookupEngine.LookupName(
            query,
            CreateOptions(false, false, true, null));

        Assert.Multiple(() =>
        {
            Assert.That(strict.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(normalized.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(normalized.Entry?.Set3Code, Is.EqualTo("aae"));
            Assert.That(
                normalized.Candidates[0].MatchedValue,
                Is.EqualTo("Albanian, Arb\u00ebresh\u00eb"));
            Assert.That(
                normalized.Candidates[0].Kind,
                Is.EqualTo(LanguageMatchKind.NormalizedName));
            Assert.That(normalized.NormalizedQuery, Is.EqualTo("ALBANIAN ARB\u00cbRESH\u00cb"));
        });
    }

    [Test]
    public void NameLanguageFilterSelectsOfficialFrenchNames()
    {
        LanguageLookupResult french = LanguageLookupEngine.LookupName(
            "anglais",
            CreateOptions(false, false, false, "fr"));
        LanguageLookupResult excluded = LanguageLookupEngine.LookupName(
            "anglais",
            CreateOptions(false, false, false, "en"));

        Assert.Multiple(() =>
        {
            Assert.That(french.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(french.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(
                french.Entry?.Names.Any(
                    name => name.Kind == LanguageNameKind.French &&
                        name.NameLanguage == "fr" && name.Text == "anglais"),
                Is.True);
            Assert.That(excluded.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(excluded.Candidates, Is.Empty);
        });
    }

    [TestCase(ReconciledSource.Iso639Set3, LanguageNameKind.Reference, "English")]
    [TestCase(ReconciledSource.Iso639Set3, LanguageNameKind.Print, "English")]
    [TestCase(ReconciledSource.Iso639Set3, LanguageNameKind.Inverted, "English")]
    [TestCase(ReconciledSource.Iso639Set2, LanguageNameKind.English, "English")]
    [TestCase(ReconciledSource.Iso639Set2, LanguageNameKind.French, "anglais")]
    [TestCase(ReconciledSource.Iana, LanguageNameKind.IanaDescription, "English")]
    public void SourceAndKindFiltersCoverEveryNameCategory(
        int sourceValue,
        LanguageNameKind kind,
        string name)
    {
        ReconciledSource source = (ReconciledSource)sourceValue;
        string nameLanguage = kind == LanguageNameKind.French ? "fr" : "en";
        LanguageLookupResult result = LanguageLookupEngine.LookupNameFiltered(
            name,
            CreateOptions(false, false, false, nameLanguage),
            source,
            true,
            kind,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(result.Candidates, Has.Length.EqualTo(1));
            Assert.That(result.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.ExactName));
        });
    }

    [Test]
    public void NormalizedCollisionsAreAmbiguousUntilLanguageFilteringResolvesThem()
    {
        LanguageRow[] languages =
        {
            CreateRow("set3:aaa", "aaa"),
            CreateRow("set3:bbb", "bbb")
        };
        NameRow[] names =
        {
            CreateName(
                "set3:aaa",
                "Shared name",
                "en",
                LanguageNameKind.Reference,
                ReconciledSource.Iso639Set3,
                true),
            CreateName(
                "set3:aaa",
                "SHARED NAME",
                "en",
                LanguageNameKind.Print,
                ReconciledSource.Iso639Set3,
                false),
            CreateName(
                "set3:bbb",
                "Shared-name",
                "fr",
                LanguageNameKind.French,
                ReconciledSource.Iso639Set2,
                false)
        };
        Dictionary<string, NameIndexEntry[]> index = NameIndexBuilder.BuildIndex(languages, names);
        LanguageCatalogData catalog = LanguageCatalogBuilder.BuildCatalog(
            languages,
            names,
            Array.Empty<AliasRow>(),
            Array.Empty<RelationshipRow>(),
            Array.Empty<RetirementRow>());
        LanguageLookupOptions unrestricted = CreateOptions(false, false, true, null);
        LanguageLookupOptions frenchOnly = CreateOptions(false, false, true, "fr");

        LanguageLookupResult ambiguous = LanguageLookupEngine.BuildNameLookupResult(
            "Shared name",
            "SHARED NAME",
            index["SHARED NAME"],
            unrestricted,
            ReconciledSource.Unknown,
            false,
            LanguageNameKind.Reference,
            false,
            catalog);
        LanguageLookupResult resolved = LanguageLookupEngine.BuildNameLookupResult(
            "Shared name",
            "SHARED NAME",
            index["SHARED NAME"],
            frenchOnly,
            ReconciledSource.Unknown,
            false,
            LanguageNameKind.Reference,
            false,
            catalog);

        Assert.Multiple(() =>
        {
            Assert.That(ambiguous.Status, Is.EqualTo(LanguageLookupStatus.Ambiguous));
            Assert.That(ambiguous.Entry, Is.Null);
            Assert.That(ambiguous.Candidates, Has.Length.EqualTo(2));
            Assert.That(
                ambiguous.Candidates.Select(candidate => candidate.Entry?.Set3Code),
                Is.EqualTo(new[] { "aaa", "bbb" }));
            Assert.That(ambiguous.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.ExactName));
            Assert.That(resolved.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(resolved.Entry?.Set3Code, Is.EqualTo("bbb"));
            Assert.That(resolved.Candidates, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void TrimmingIsIndependentFromFullNameNormalization()
    {
        LanguageLookupResult trimmed = LanguageLookupEngine.LookupName(
            "  English  ",
            CreateOptions(true, false, false, null));
        LanguageLookupResult untrimmed = LanguageLookupEngine.LookupName(
            "  English  ",
            CreateOptions(false, false, false, null));

        Assert.Multiple(() =>
        {
            Assert.That(trimmed.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(trimmed.NormalizedQuery, Is.EqualTo("English"));
            Assert.That(trimmed.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.ExactName));
            Assert.That(untrimmed.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(untrimmed.NormalizedQuery, Is.EqualTo("  English  "));
        });
    }

    [Test]
    public void EmptyNormalizedAndUnknownNamesHaveDistinctOutcomes()
    {
        LanguageLookupResult nullName = LanguageLookupEngine.LookupName(
            null,
            CreateOptions(false, false, false, null));
        LanguageLookupResult empty = LanguageLookupEngine.LookupName(
            string.Empty,
            CreateOptions(false, false, false, null));
        LanguageLookupResult punctuation = LanguageLookupEngine.LookupName(
            "---",
            CreateOptions(false, false, true, null));
        LanguageLookupResult unknown = LanguageLookupEngine.LookupName(
            "Definitely Not A Registered Language Name",
            CreateOptions(false, false, true, null));

        Assert.Multiple(() =>
        {
            Assert.That(nullName.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(nullName.NormalizedQuery, Is.Null);
            Assert.That(empty.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(punctuation.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(punctuation.NormalizedQuery, Is.Empty);
            Assert.That(unknown.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(unknown.Candidates, Is.Empty);
        });
    }

    [Test]
    public void NameLookupResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageLookupOptions options = CreateOptions(false, false, false, null);
        LanguageLookupResult first = LanguageLookupEngine.LookupName("English", options);
        LanguageLookupResult second = LanguageLookupEngine.LookupName("English", options);
        string originalName = second.Entry!.Names[0].Text!;

        first.Entry!.Names[0].Text = "changed";
        first.Candidates[0].MatchedValue = "changed";
        first.Candidates[0].Entry!.Set3Code = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first.Entry, Is.Not.SameAs(second.Entry));
            Assert.That(first.Candidates[0], Is.Not.SameAs(second.Candidates[0]));
            Assert.That(second.Entry?.Names[0].Text, Is.EqualTo(originalName));
            Assert.That(second.Candidates[0].MatchedValue, Is.EqualTo("English"));
            Assert.That(second.Candidates[0].Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(
                LanguageLookupEngine.LookupName("English", options).Entry?.Set3Code,
                Is.EqualTo("eng"));
        });
    }

    private static LanguageLookupOptions CreateOptions(
        bool trimWhitespace,
        bool normalizeCase,
        bool normalizeNames,
        string? nameLanguage)
    {
        return new LanguageLookupOptions
        {
            TrimWhitespace = trimWhitespace,
            NormalizeCase = normalizeCase,
            NormalizeNames = normalizeNames,
            NameLanguage = nameLanguage
        };
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
        string nameLanguage,
        LanguageNameKind kind,
        ReconciledSource source,
        bool isReference)
    {
        return new NameRow(
            entityKey,
            text,
            nameLanguage,
            kind,
            source,
            isReference);
    }
}
