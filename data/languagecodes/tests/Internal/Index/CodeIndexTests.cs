using languagecodes;

namespace languagecodes.Tests;

public class CodeIndexTests
{
    [Test]
    public void ActiveCodesRetainEveryCodeFormForTheSameEntity()
    {
        CodeIndexEntry[] french = CodeIndex.FindAll("fra");

        Assert.Multiple(() =>
        {
            Assert.That(french.Any(entry => entry.Form == LanguageCodeForm.Set2Terminology), Is.True);
            Assert.That(french.Any(entry => entry.Form == LanguageCodeForm.Set3), Is.True);
            Assert.That(french.All(entry => entry.Status == LanguageCodeStatus.Active), Is.True);
            Assert.That(
                french.Select(entry => entry.EntityIndex).Distinct().Count(),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void RetiredCodesPreserveSingularAndSplitReplacementOutcomes()
    {
        CodeIndexEntry? singular = CodeIndex.FindExact("fri", LanguageCodeForm.RetiredSet3);
        CodeIndexEntry? split = CodeIndex.FindExact("agp", LanguageCodeForm.RetiredSet3);

        Assert.Multiple(() =>
        {
            Assert.That(singular, Is.Not.Null);
            Assert.That(singular?.Status, Is.EqualTo(LanguageCodeStatus.Retired));
            Assert.That(singular?.PreferredValue, Is.EqualTo("fry"));
            Assert.That(singular?.EntityIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(split, Is.Not.Null);
            Assert.That(split?.Status, Is.EqualTo(LanguageCodeStatus.Retired));
            Assert.That(split?.PreferredValue, Is.Null);
            Assert.That(split?.EntityIndex, Is.EqualTo(-1));
        });
    }

    [Test]
    public void PrivateUseRangeIncludesBothBoundariesAndExcludesNeighbors()
    {
        CodeIndexEntry? first = CodeIndex.FindExact("qaa", LanguageCodeForm.PrivateUse);
        CodeIndexEntry? last = CodeIndex.FindExact("qtz", LanguageCodeForm.PrivateUse);

        Assert.Multiple(() =>
        {
            Assert.That(first?.Status, Is.EqualTo(LanguageCodeStatus.PrivateUse));
            Assert.That(first?.Set, Is.EqualTo(Iso639Set.Set3));
            Assert.That(first?.EntityIndex, Is.EqualTo(-1));
            Assert.That(last?.Status, Is.EqualTo(LanguageCodeStatus.PrivateUse));
            Assert.That(CodeIndex.FindExact("qua", LanguageCodeForm.PrivateUse), Is.Null);
            Assert.That(CodeIndex.FindExact("qzz", LanguageCodeForm.PrivateUse), Is.Null);
        });
    }

    [Test]
    public void IanaAliasesRetainPreferredValuesAndRecordForms()
    {
        CodeIndexEntry? deprecated = CodeIndex.FindExact("iw", LanguageCodeForm.IanaLanguage);
        CodeIndexEntry? extlang = CodeIndex.FindExact("aao", LanguageCodeForm.IanaExtlang);
        CodeIndexEntry? deprecatedExtlang = CodeIndex.FindExact("ajp", LanguageCodeForm.IanaExtlang);

        Assert.Multiple(() =>
        {
            Assert.That(deprecated?.Status, Is.EqualTo(LanguageCodeStatus.Deprecated));
            Assert.That(deprecated?.PreferredValue, Is.EqualTo("he"));
            Assert.That(deprecated?.EntityIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(extlang?.Status, Is.EqualTo(LanguageCodeStatus.Active));
            Assert.That(extlang?.PreferredValue, Is.EqualTo("aao"));
            Assert.That(extlang?.EntityIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(deprecatedExtlang?.Status, Is.EqualTo(LanguageCodeStatus.Deprecated));
            Assert.That(deprecatedExtlang?.PreferredValue, Is.EqualTo("ajp"));
        });
    }

    [Test]
    public void CompleteIanaAliasesUseCompleteTagForms()
    {
        CodeIndexEntry? grandfathered =
            CodeIndex.FindExact("i-klingon", LanguageCodeForm.GrandfatheredTag);
        CodeIndexEntry? redundant =
            CodeIndex.FindExact("sgn-US", LanguageCodeForm.RedundantTag);

        Assert.Multiple(() =>
        {
            Assert.That(grandfathered?.PreferredValue, Is.EqualTo("tlh"));
            Assert.That(grandfathered?.Status, Is.EqualTo(LanguageCodeStatus.Deprecated));
            Assert.That(grandfathered?.EntityIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(redundant?.PreferredValue, Is.EqualTo("ase"));
            Assert.That(redundant?.Status, Is.EqualTo(LanguageCodeStatus.Deprecated));
            Assert.That(redundant?.EntityIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(
                CodeIndex.FindExact("i-klingon", LanguageCodeForm.IanaLanguage),
                Is.Null);
        });
    }

    [Test]
    public void IndexUsesOrdinalCaseSensitiveKeys()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CodeIndex.FindAll("eng"), Is.Not.Empty);
            Assert.That(CodeIndex.FindAll("ENG"), Is.Empty);
            Assert.That(CodeIndex.EntriesByValue.Comparer, Is.SameAs(StringComparer.Ordinal));
        });
    }

    [Test]
    public void FindAllReturnsDefensiveEntrySnapshots()
    {
        CodeIndexEntry[] first = CodeIndex.FindAll("eng");
        CodeIndexEntry[] second = CodeIndex.FindAll("eng");
        LanguageCodeStatus originalStatus = second[0].Status;

        first[0].Status = LanguageCodeStatus.Unassigned;

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first[0], Is.Not.SameAs(second[0]));
            Assert.That(CodeIndex.FindAll("eng")[0].Status, Is.EqualTo(originalStatus));
        });
    }

    [Test]
    public void ConflictingSameFormMappingsFailIndexConstruction()
    {
        LanguageRow[] languages =
        {
            CreateLanguageRow("set3:aaa", "aaa"),
            CreateLanguageRow("set3:bbb", "bbb")
        };
        CodeIndexSeedRow[] seeds =
        {
            new CodeIndexSeedRow("abc", 0, LanguageCodeForm.Set3, Iso639Set.Set3),
            new CodeIndexSeedRow("abc", 1, LanguageCodeForm.Set3, Iso639Set.Set3)
        };

        Assert.Throws<InvalidOperationException>(
            () => CodeIndexBuilder.BuildIndex(
                languages,
                seeds,
                Array.Empty<RetirementRow>(),
                Array.Empty<AliasRow>()));
    }

    [Test]
    public void IdenticalSameFormMappingsAreDeduplicated()
    {
        LanguageRow[] languages = { CreateLanguageRow("set3:aaa", "aaa") };
        CodeIndexSeedRow seed = new CodeIndexSeedRow(
            "aaa",
            0,
            LanguageCodeForm.Set3,
            Iso639Set.Set3);

        Dictionary<string, CodeIndexEntry[]> index = CodeIndexBuilder.BuildIndex(
            languages,
            new[] { seed, seed },
            Array.Empty<RetirementRow>(),
            Array.Empty<AliasRow>());

        Assert.That(
            index["aaa"].Count(entry => entry.Form == LanguageCodeForm.Set3),
            Is.EqualTo(1));
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
