using languagecodes;

namespace languagecodes.Tests;

public class NameIndexTests
{
    [Test]
    public void ExactLookupUsesDeterministicUnicodeNameNormalization()
    {
        NameIndexEntry[] matches = NameIndex.FindExact("  english!!! ");

        Assert.Multiple(() =>
        {
            Assert.That(matches, Is.Not.Empty);
            Assert.That(matches.All(entry => entry.NormalizedValue == "ENGLISH"), Is.True);
            Assert.That(matches.All(entry => entry.EntityKey == "set3:eng"), Is.True);
            Assert.That(matches.Any(entry => entry.Kind == LanguageNameKind.Reference), Is.True);
            Assert.That(matches.Any(entry => entry.Kind == LanguageNameKind.IanaDescription), Is.True);
            Assert.That(matches.Any(entry => entry.Source == ReconciledSource.Iana), Is.True);
        });
    }

    [Test]
    public void MultimapRetainsKindsSourcesAndCrossEntityCollisions()
    {
        LanguageRow[] languages =
        {
            CreateLanguageRow("set3:aaa", "aaa"),
            CreateLanguageRow("set3:bbb", "bbb")
        };
        NameRow[] names =
        {
            new NameRow(
                "set3:bbb",
                "Shared-name",
                "fr",
                LanguageNameKind.French,
                ReconciledSource.Iso639Set2,
                false),
            new NameRow(
                "set3:aaa",
                "shared name",
                "en",
                LanguageNameKind.Reference,
                ReconciledSource.Iso639Set3,
                true),
            new NameRow(
                "set3:aaa",
                "SHARED  NAME",
                "en",
                LanguageNameKind.IanaDescription,
                ReconciledSource.Iana,
                false)
        };

        Dictionary<string, NameIndexEntry[]> index = NameIndexBuilder.BuildIndex(languages, names);
        NameIndexEntry[] matches = index["SHARED NAME"];

        Assert.Multiple(() =>
        {
            Assert.That(matches, Has.Length.EqualTo(3));
            Assert.That(matches.Select(entry => entry.EntityKey).Distinct().Count(), Is.EqualTo(2));
            Assert.That(matches.Any(entry => entry.NameLanguage == "fr"), Is.True);
            Assert.That(matches.Any(entry => entry.Kind == LanguageNameKind.Reference), Is.True);
            Assert.That(matches.Any(entry => entry.Kind == LanguageNameKind.French), Is.True);
            Assert.That(matches.Any(entry => entry.Source == ReconciledSource.Iana), Is.True);
            Assert.That(matches.Any(entry => entry.IsReferenceName), Is.True);
        });
    }

    [Test]
    public void PrefixAndContainsTraversalAreStableAndLimited()
    {
        NameIndexEntry[] firstPrefix = NameIndex.Search("old eng", LanguageSearchMode.Prefix, 8);
        NameIndexEntry[] secondPrefix = NameIndex.Search("old eng", LanguageSearchMode.Prefix, 8);
        NameIndexEntry[] firstContains = NameIndex.Search("english", LanguageSearchMode.Contains, 12);
        NameIndexEntry[] secondContains = NameIndex.Search("english", LanguageSearchMode.Contains, 12);

        Assert.Multiple(() =>
        {
            Assert.That(firstPrefix, Is.Not.Empty);
            Assert.That(firstPrefix, Has.Length.LessThanOrEqualTo(8));
            Assert.That(firstPrefix.All(entry => entry.NormalizedValue.StartsWith("OLD ENG")), Is.True);
            Assert.That(
                firstPrefix.Select(StableIdentity),
                Is.EqualTo(secondPrefix.Select(StableIdentity)));
            Assert.That(firstContains, Is.Not.Empty);
            Assert.That(firstContains, Has.Length.LessThanOrEqualTo(12));
            Assert.That(firstContains.All(entry => entry.NormalizedValue.Contains("ENGLISH")), Is.True);
            Assert.That(
                firstContains.Select(StableIdentity),
                Is.EqualTo(secondContains.Select(StableIdentity)));
        });
    }

    [Test]
    public void ExactSearchHonorsLimitsAndEmptyInputRules()
    {
        NameIndexEntry[] limited = NameIndex.Search("English", LanguageSearchMode.Exact, 2);

        Assert.Multiple(() =>
        {
            Assert.That(limited, Has.Length.EqualTo(2));
            Assert.That(limited.All(entry => entry.NormalizedValue == "ENGLISH"), Is.True);
            Assert.That(NameIndex.Search("English", LanguageSearchMode.Exact, 0), Is.Empty);
            Assert.That(NameIndex.Search(string.Empty, LanguageSearchMode.Prefix, 10), Is.Empty);
            Assert.That(NameIndex.FindExact("---"), Is.Empty);
        });
    }

    [Test]
    public void ExactLookupReturnsDefensiveEntrySnapshots()
    {
        NameIndexEntry[] first = NameIndex.FindExact("English");
        NameIndexEntry[] second = NameIndex.FindExact("English");
        string originalText = second[0].Text;

        first[0].Text = "Changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first[0], Is.Not.SameAs(second[0]));
            Assert.That(NameIndex.FindExact("English")[0].Text, Is.EqualTo(originalText));
        });
    }

    [Test]
    public void IndexUsesOrdinalKeysAndSortedTraversalValues()
    {
        Assert.That(NameIndex.EntriesByNormalizedValue.Comparer, Is.SameAs(StringComparer.Ordinal));
        for (int index = 1; index < NameIndex.SortedNormalizedValues.Length; index++)
        {
            Assert.That(
                string.CompareOrdinal(
                    NameIndex.SortedNormalizedValues[index - 1],
                    NameIndex.SortedNormalizedValues[index]),
                Is.LessThanOrEqualTo(0));
        }
    }

    [Test]
    public void MissingEntityReferencesFailIndexConstruction()
    {
        NameRow[] names =
        {
            new NameRow(
                "set3:missing",
                "Missing",
                "en",
                LanguageNameKind.Reference,
                ReconciledSource.Iso639Set3,
                true)
        };

        Assert.Throws<InvalidOperationException>(
            () => NameIndexBuilder.BuildIndex(
                new[] { CreateLanguageRow("set3:aaa", "aaa") },
                names));
    }

    private static string StableIdentity(NameIndexEntry entry)
    {
        return entry.NormalizedValue + "|" + entry.EntityKey + "|" + entry.NameLanguage + "|" +
            entry.Kind + "|" + entry.Source + "|" + entry.NameIndex;
    }

    private static LanguageRow CreateLanguageRow(string key, string code)
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
}
