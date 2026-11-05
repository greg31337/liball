using languagecodes;

namespace languagecodes.Tests;

public class LanguageCatalogEngineTests
{
    [Test]
    public void AllEntriesAreActiveAndUseStableOrdinalCodeOrder()
    {
        LanguageCodeEntry[] entries = LanguageCatalogEngine.GetAllEntries();

        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Length.EqualTo(GeneratedDataMetadata.ActiveEntityCount));
            Assert.That(entries.All(entry => entry.Status == LanguageCodeStatus.Active), Is.True);
            Assert.That(
                entries.Select(StableCode),
                Is.EqualTo(entries.Select(StableCode).OrderBy(code => code, StringComparer.Ordinal)));
            Assert.That(
                LanguageCatalogEngine.Data.EntityIndexesByKey.Comparer,
                Is.SameAs(StringComparer.Ordinal));
        });
    }

    [Test]
    public void SetScopeAndTypeFiltersReturnOnlyMatchingActiveEntries()
    {
        LanguageCodeEntry[] set1 = LanguageCatalogEngine.GetEntriesBySet(Iso639Set.Set1);
        LanguageCodeEntry[] set2 = LanguageCatalogEngine.GetEntriesBySet(Iso639Set.Set2);
        LanguageCodeEntry[] set3 = LanguageCatalogEngine.GetEntriesBySet(Iso639Set.Set3);
        LanguageCodeEntry[] set5 = LanguageCatalogEngine.GetEntriesBySet(Iso639Set.Set5);
        LanguageCodeEntry[] macros =
            LanguageCatalogEngine.GetEntriesByScope(LanguageScope.Macrolanguage);
        LanguageCodeEntry[] historical =
            LanguageCatalogEngine.GetEntriesByType(LanguageType.Historical);

        Assert.Multiple(() =>
        {
            Assert.That(set1, Has.Length.EqualTo(GeneratedDataMetadata.Set1RecordCount));
            Assert.That(set2, Has.Length.EqualTo(GeneratedDataMetadata.Set2RecordCount));
            Assert.That(set3, Has.Length.EqualTo(GeneratedDataMetadata.Set3RecordCount));
            Assert.That(set5, Has.Length.EqualTo(GeneratedDataMetadata.Set5RecordCount));
            Assert.That(set1.All(entry => entry.InSet1), Is.True);
            Assert.That(set2.All(entry => entry.InSet2), Is.True);
            Assert.That(set3.All(entry => entry.InSet3), Is.True);
            Assert.That(set5.All(entry => entry.InSet5), Is.True);
            Assert.That(macros, Is.Not.Empty);
            Assert.That(macros.All(entry => entry.Scope == LanguageScope.Macrolanguage), Is.True);
            Assert.That(macros.Select(entry => entry.Set3Code), Does.Contain("ara"));
            Assert.That(historical, Is.Not.Empty);
            Assert.That(
                historical.All(entry => entry.LanguageType == LanguageType.Historical),
                Is.True);
            Assert.That(LanguageCatalogEngine.GetEntriesBySet(Iso639Set.None), Is.Empty);
        });
    }

    [Test]
    public void CatalogEntriesComposeNamesAliasesAndDirectRelationships()
    {
        int aaoIndex = LanguageCatalogEngine.Data.EntityIndexesByKey["set3:aao"];
        int englishIndex = LanguageCatalogEngine.Data.EntityIndexesByKey["set3:eng"];
        LanguageCodeEntry? aao = LanguageCatalogEngine.GetEntryByEntityIndex(aaoIndex);
        LanguageCodeEntry? english = LanguageCatalogEngine.GetEntryByEntityIndex(englishIndex);

        Assert.Multiple(() =>
        {
            Assert.That(aao?.ReferenceName, Is.EqualTo("Algerian Saharan Arabic"));
            Assert.That(aao?.Names.Any(name => name.IsReferenceName), Is.True);
            Assert.That(
                aao?.Aliases.Any(
                    alias => alias.Kind == LanguageAliasKind.IanaPreferred && alias.Value == "aao"),
                Is.True);
            Assert.That(
                aao?.Relationships.Any(
                    relationship => relationship.Kind == LanguageRelationshipKind.MacrolanguageMember &&
                        relationship.ParentCode == "ara" && relationship.ChildCode == "aao"),
                Is.True);
            Assert.That(english?.Alpha2Code, Is.EqualTo("en"));
            Assert.That(english?.Set2TerminologyCode, Is.EqualTo("eng"));
            Assert.That(english?.Set3Code, Is.EqualTo("eng"));
            Assert.That(english?.InSet1, Is.True);
            Assert.That(english?.InSet2, Is.True);
            Assert.That(english?.InSet3, Is.True);
            Assert.That(LanguageCatalogEngine.GetEntryByEntityIndex(-1), Is.Null);
            Assert.That(LanguageCatalogEngine.GetEntryByEntityIndex(int.MaxValue), Is.Null);
        });
    }

    [Test]
    public void StatusFilteringIncludesAuthoritativeRetiredSet3Snapshots()
    {
        LanguageCodeEntry[] active =
            LanguageCatalogEngine.GetEntriesByStatus(LanguageCodeStatus.Active);
        LanguageCodeEntry[] retired =
            LanguageCatalogEngine.GetEntriesByStatus(LanguageCodeStatus.Retired);
        LanguageCodeEntry split = retired.Single(entry => entry.Set3Code == "agp");
        LanguageCodeEntry historicalMember = retired.Single(entry => entry.Set3Code == "ajp");

        Assert.Multiple(() =>
        {
            Assert.That(active, Has.Length.EqualTo(GeneratedDataMetadata.ActiveEntityCount));
            Assert.That(retired, Has.Length.EqualTo(GeneratedDataMetadata.RetiredCodeCount));
            Assert.That(retired.All(entry => entry.Status == LanguageCodeStatus.Retired), Is.True);
            Assert.That(retired.All(entry => entry.InSet3), Is.True);
            Assert.That(
                retired.Select(StableCode),
                Is.EqualTo(retired.Select(StableCode).OrderBy(code => code, StringComparer.Ordinal)));
            Assert.That(split.ReferenceName, Is.EqualTo("Paranan"));
            Assert.That(split.Retirement?.Reason, Is.EqualTo(LanguageRetirementReason.Split));
            Assert.That(split.Retirement?.ReplacementCodes, Is.EqualTo(new[] { "apf", "prf" }));
            Assert.That(
                historicalMember.Relationships.Any(
                    relationship => relationship.ParentCode == "ara" &&
                        relationship.ChildCode == "ajp" && relationship.IsRetired),
                Is.True);
            Assert.That(
                LanguageCatalogEngine.Data.RetiredEntriesByCode.Comparer,
                Is.SameAs(StringComparer.Ordinal));
        });
    }

    [Test]
    public void EveryCatalogExitReturnsARecursiveDefensiveSnapshot()
    {
        int entityIndex = LanguageCatalogEngine.Data.EntityIndexesByKey["set3:aao"];
        LanguageCodeEntry first = LanguageCatalogEngine.GetEntryByEntityIndex(entityIndex)!;
        LanguageCodeEntry second = LanguageCatalogEngine.GetEntryByEntityIndex(entityIndex)!;
        string storedName = second.Names[0].Text!;
        string storedAlias = second.Aliases[0].Value!;
        string storedParent = second.Relationships[0].ParentCode!;

        first.Names[0].Text = "changed";
        first.Aliases[0].Value = "changed";
        first.Relationships[0].ParentCode = "changed";
        LanguageCodeEntry[] filtered =
            LanguageCatalogEngine.GetEntriesByScope(LanguageScope.Individual);
        LanguageCodeEntry filteredAao = filtered.Single(entry => entry.Set3Code == "aao");
        filteredAao.Names[0].Text = "also changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Names[0], Is.Not.SameAs(second.Names[0]));
            Assert.That(first.Aliases[0], Is.Not.SameAs(second.Aliases[0]));
            Assert.That(first.Relationships[0], Is.Not.SameAs(second.Relationships[0]));
            Assert.That(LanguageCatalogEngine.GetEntryByEntityIndex(entityIndex)?.Names[0].Text,
                Is.EqualTo(storedName));
            Assert.That(LanguageCatalogEngine.GetEntryByEntityIndex(entityIndex)?.Aliases[0].Value,
                Is.EqualTo(storedAlias));
            Assert.That(LanguageCatalogEngine.GetEntryByEntityIndex(entityIndex)?.Relationships[0].ParentCode,
                Is.EqualTo(storedParent));
        });
    }

    [Test]
    public void StatusFilteringAndSortingWorkForFixtureCatalogs()
    {
        LanguageRow activeLast = CreateRow(
            "set3:ccc",
            "ccc",
            LanguageScope.Individual,
            LanguageType.Living,
            LanguageCodeStatus.Active);
        LanguageRow retired = CreateRow(
            "set3:bbb",
            "bbb",
            LanguageScope.Individual,
            LanguageType.Extinct,
            LanguageCodeStatus.Retired);
        LanguageRow activeFirst = CreateRow(
            "set3:aaa",
            "aaa",
            LanguageScope.Macrolanguage,
            LanguageType.Living,
            LanguageCodeStatus.Active);
        RetirementRow retirement = new RetirementRow(
            "bbb",
            "Retired B",
            LanguageRetirementReason.Change,
            new[] { "ccc" },
            null,
            "2000-01-01");
        LanguageCatalogData catalog = LanguageCatalogBuilder.BuildCatalog(
            new[] { activeLast, retired, activeFirst },
            new[]
            {
                CreateName("set3:ccc", "C"),
                CreateName("set3:bbb", "B"),
                CreateName("set3:aaa", "A")
            },
            Array.Empty<AliasRow>(),
            Array.Empty<RelationshipRow>(),
            new[] { retirement });
        activeFirst.Set3Code = "changed";
        retirement.ReplacementCodes[0] = "changed";

        LanguageCodeEntry[] active = LanguageCatalogBuilder.CopyActiveEntries(catalog);
        LanguageCodeEntry[] retiredEntries =
            LanguageCatalogBuilder.CopyEntriesWithStatus(catalog, LanguageCodeStatus.Retired);

        Assert.Multiple(() =>
        {
            Assert.That(active.Select(entry => entry.Set3Code), Is.EqualTo(new[] { "aaa", "ccc" }));
            Assert.That(retiredEntries, Has.Length.EqualTo(1));
            Assert.That(retiredEntries[0].Set3Code, Is.EqualTo("bbb"));
            Assert.That(retiredEntries[0].Retirement?.ReplacementCodes, Is.EqualTo(new[] { "ccc" }));
            Assert.That(
                LanguageCatalogBuilder.CopyEntriesInScope(catalog, LanguageScope.Macrolanguage)
                    .Select(entry => entry.Set3Code),
                Is.EqualTo(new[] { "aaa" }));
            Assert.That(
                LanguageCatalogBuilder.CopyEntriesOfType(catalog, LanguageType.Extinct),
                Is.Empty);
        });
    }

    [Test]
    public void DuplicateEntityKeysFailCatalogConstruction()
    {
        LanguageRow first = CreateRow(
            "set3:aaa",
            "aaa",
            LanguageScope.Individual,
            LanguageType.Living,
            LanguageCodeStatus.Active);
        LanguageRow second = CreateRow(
            "set3:aaa",
            "bbb",
            LanguageScope.Individual,
            LanguageType.Living,
            LanguageCodeStatus.Active);

        Assert.Throws<InvalidOperationException>(
            () => LanguageCatalogBuilder.BuildCatalog(
                new[] { first, second },
                Array.Empty<NameRow>(),
                Array.Empty<AliasRow>(),
                Array.Empty<RelationshipRow>(),
                Array.Empty<RetirementRow>()));
    }

    private static string StableCode(LanguageCodeEntry entry)
    {
        return entry.Set3Code ?? entry.Set5Code ?? entry.Set2TerminologyCode ??
            entry.Set2BibliographicCode ?? entry.Alpha2Code ?? string.Empty;
    }

    private static LanguageRow CreateRow(
        string key,
        string code,
        LanguageScope scope,
        LanguageType type,
        LanguageCodeStatus status)
    {
        return new LanguageRow(
            key,
            null,
            null,
            null,
            code,
            null,
            scope,
            type,
            status,
            ReconciledGroupKind.None,
            null);
    }

    private static NameRow CreateName(string entityKey, string text)
    {
        return new NameRow(
            entityKey,
            text,
            "en",
            LanguageNameKind.Reference,
            ReconciledSource.Iso639Set3,
            true);
    }
}
