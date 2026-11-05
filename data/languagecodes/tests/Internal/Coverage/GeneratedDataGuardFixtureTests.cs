using languagecodes;

namespace languagecodes.Tests;

public sealed class GeneratedDataGuardFixtureTests
{
    [Test]
    public void DuplicateGeneratedEntityKeysFailEveryEntityIndexBuilder()
    {
        LanguageRow duplicate = CreateLanguageRow("set3:aaa", "aaa");
        LanguageRow[] rows = { duplicate, CreateLanguageRow("set3:aaa", "bbb") };

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => CodeIndexBuilder.BuildEntityIndexes(rows));
            Assert.Throws<InvalidOperationException>(() => NameIndexBuilder.BuildNameEntityIndexes(rows));
        });
    }

    [Test]
    public void ConflictingGeneratedRetirementTargetsFailCodeIndexConstruction()
    {
        AliasRow[] aliases =
        {
            CreateRetiredAlias("set3:aaa", "old"),
            CreateRetiredAlias("set3:bbb", "old")
        };

        InvalidOperationException? exception = Assert.Throws<InvalidOperationException>(
            () => CodeIndexBuilder.FindRetiredEntityKey("old", aliases));

        Assert.That(exception?.Message, Does.Contain("Conflicting retired-code targets"));
    }

    [Test]
    public void ConflictingGeneratedCodeStatusesFailUnlessTheyAreCompatibleIanaStates()
    {
        Dictionary<string, List<CodeIndexEntry>> conflicting =
            new Dictionary<string, List<CodeIndexEntry>>(StringComparer.Ordinal);
        CodeIndexBuilder.AddEntry(
            conflicting,
            new CodeIndexEntry(
                "abc",
                0,
                LanguageCodeForm.Set3,
                Iso639Set.Set3,
                LanguageCodeStatus.Active,
                null));

        Assert.Throws<InvalidOperationException>(() => CodeIndexBuilder.AddEntry(
            conflicting,
            new CodeIndexEntry(
                "abc",
                0,
                LanguageCodeForm.Set3,
                Iso639Set.Set3,
                LanguageCodeStatus.Retired,
                null)));

        Dictionary<string, List<CodeIndexEntry>> compatible =
            new Dictionary<string, List<CodeIndexEntry>>(StringComparer.Ordinal);
        CodeIndexBuilder.AddEntry(
            compatible,
            new CodeIndexEntry(
                "abc",
                0,
                LanguageCodeForm.IanaLanguage,
                Iso639Set.None,
                LanguageCodeStatus.Deprecated,
                "def"));
        CodeIndexBuilder.AddEntry(
            compatible,
            new CodeIndexEntry(
                "abc",
                0,
                LanguageCodeForm.IanaLanguage,
                Iso639Set.None,
                LanguageCodeStatus.Active,
                "def"));

        Assert.That(compatible["abc"][0].Status, Is.EqualTo(LanguageCodeStatus.Deprecated));
    }

    [Test]
    public void InvalidAndRepeatedGeneratedIanaSeedsFailIndexConstruction()
    {
        IanaSubtagRow record = CreateIanaRecord("testvar");
        IanaIndexSeedRow valid = new IanaIndexSeedRow(
            LanguageSubtagType.Variant,
            "testvar",
            0);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => SubtagIndexBuilder.BuildIndex(
                new[] { record },
                new[]
                {
                    new IanaIndexSeedRow(LanguageSubtagType.Variant, "testvar", -1)
                }));
            Assert.Throws<InvalidOperationException>(() => SubtagIndexBuilder.BuildIndex(
                new[] { record },
                new[]
                {
                    new IanaIndexSeedRow(LanguageSubtagType.Variant, "testvar", 1)
                }));
            Assert.Throws<InvalidOperationException>(() => SubtagIndexBuilder.BuildIndex(
                new[] { record },
                new[] { valid, valid }));
        });
    }

    [Test]
    public void DuplicateGeneratedRetirementRowsFailCatalogIndexes()
    {
        RetirementRow retirement = CreateRetirement("old");
        RetirementRow[] duplicates = { retirement, CreateRetirement("old") };
        Dictionary<string, List<RelationshipRow>> relationships =
            new Dictionary<string, List<RelationshipRow>>(StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(
                () => LanguageCatalogBuilder.BuildRetirementRecords(duplicates));
            Assert.Throws<InvalidOperationException>(() =>
                LanguageCatalogBuilder.BuildRetiredEntryIndex(
                    Array.Empty<LanguageRow>(),
                    duplicates,
                    relationships));
        });
    }

    [Test]
    public void RevisitedGeneratedGroupNodeTerminatesCycleSearch()
    {
        Dictionary<string, RelationshipIndexEntry[]> graph =
            new Dictionary<string, RelationshipIndexEntry[]>(StringComparer.Ordinal)
            {
                ["aaa"] = new[] { CreateGroupRelationship("aaa", "bbb") },
                ["bbb"] = new[] { CreateGroupRelationship("bbb", "aaa") }
            };
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);

        bool found = RelationshipIndexBuilder.HasGroupPath("aaa", "zzz", graph, visited);

        Assert.That(found, Is.False);
    }

    [Test]
    public void StableCodeFallbacksRemainDefinedForMalformedGeneratedRows()
    {
        LanguageRow bibliographic = new LanguageRow(
            "bibliographic",
            null,
            null,
            "bib",
            null,
            null,
            LanguageScope.Individual,
            LanguageType.Living,
            LanguageCodeStatus.Active,
            ReconciledGroupKind.None,
            null);
        LanguageRow alpha2 = new LanguageRow(
            "alpha2",
            "aa",
            null,
            null,
            null,
            null,
            LanguageScope.Individual,
            LanguageType.Living,
            LanguageCodeStatus.Active,
            ReconciledGroupKind.None,
            null);
        LanguageRow keyOnly = new LanguageRow(
            "key-only",
            null,
            null,
            null,
            null,
            null,
            LanguageScope.Unknown,
            LanguageType.Unknown,
            LanguageCodeStatus.Active,
            ReconciledGroupKind.None,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(LanguageCatalogBuilder.FindStableCode(bibliographic), Is.EqualTo("bib"));
            Assert.That(LanguageCatalogBuilder.FindStableCode(alpha2), Is.EqualTo("aa"));
            Assert.That(LanguageCatalogBuilder.FindStableCode(keyOnly), Is.EqualTo("key-only"));
            Assert.That(
                LanguageCatalogBuilder.FindStablePublicCode(
                    new LanguageCodeEntry { Set2BibliographicCode = "bib" }),
                Is.EqualTo("bib"));
            Assert.That(
                LanguageCatalogBuilder.FindStablePublicCode(
                    new LanguageCodeEntry { Alpha2Code = "aa" }),
                Is.EqualTo("aa"));
            Assert.That(
                LanguageCatalogBuilder.FindStablePublicCode(new LanguageCodeEntry()),
                Is.EqualTo(string.Empty));
        });
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

    private static AliasRow CreateRetiredAlias(string entityKey, string value)
    {
        return new AliasRow(
            entityKey,
            value,
            LanguageAliasKind.RetiredSet3,
            null,
            ReconciledSource.Iso639Set3,
            "retired:" + value,
            false);
    }

    private static IanaSubtagRow CreateIanaRecord(string value)
    {
        return new IanaSubtagRow(
            LanguageSubtagType.Variant,
            value,
            new[] { "Fixture" },
            "2000-01-01",
            null,
            null,
            Array.Empty<string>(),
            null,
            null,
            null,
            Array.Empty<string>());
    }

    private static RetirementRow CreateRetirement(string code)
    {
        return new RetirementRow(
            code,
            "Fixture",
            LanguageRetirementReason.Nonexistent,
            Array.Empty<string>(),
            null,
            "2000-01-01");
    }

    private static RelationshipIndexEntry CreateGroupRelationship(string parent, string child)
    {
        return new RelationshipIndexEntry(
            parent,
            child,
            LanguageRelationshipKind.Set5ParentChild,
            ReconciledRelationshipStatus.Active,
            ReconciledSource.Iso639Set5);
    }
}
