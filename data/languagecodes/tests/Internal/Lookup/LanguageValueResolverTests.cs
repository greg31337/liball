using languagecodes;

namespace languagecodes.Tests;

public class LanguageValueResolverTests
{
    [Test]
    public void ResolverPrefersAnActiveCodeOverAnExactName()
    {
        LanguageLookupOptions options = CreateOptions(true, false, false, false, null);

        LanguageLookupResult result = LanguageLookupEngine.ResolveValue("En", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(result.Entry?.ReferenceName, Is.EqualTo("English"));
            Assert.That(result.Candidates, Has.Length.EqualTo(1));
            Assert.That(result.Candidates[0].MatchedValue, Is.EqualTo("en"));
            Assert.That(result.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.Alpha2));
            Assert.That(
                LanguageLookupEngine.LookupName("En", options).Entry?.Set3Code,
                Is.EqualTo("enc"));
        });
    }

    [Test]
    public void ResolverAppliesActiveAliasAndRetiredCodePrecedence()
    {
        LanguageLookupResult active = LanguageLookupEngine.ResolveValue(
            "aao",
            CreateOptions(false, false, false, false, null));
        LanguageLookupResult alias = LanguageLookupEngine.ResolveValue(
            "iw",
            CreateOptions(false, false, false, false, null));
        LanguageLookupResult retired = LanguageLookupEngine.ResolveValue(
            "fri",
            CreateOptions(false, false, false, true, null));
        LanguageLookupResult excludedRetired = LanguageLookupEngine.ResolveValue(
            "fri",
            CreateOptions(false, false, false, false, null));

        Assert.Multiple(() =>
        {
            Assert.That(active.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(active.Entry?.Set3Code, Is.EqualTo("aao"));
            Assert.That(active.Candidates, Has.Length.EqualTo(1));
            Assert.That(active.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.Set3));
            Assert.That(alias.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(alias.Entry?.Set3Code, Is.EqualTo("heb"));
            Assert.That(alias.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.IanaAlias));
            Assert.That(retired.Status, Is.EqualTo(LanguageLookupStatus.Retired));
            Assert.That(retired.Entry?.Set3Code, Is.EqualTo("fri"));
            Assert.That(retired.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.RetiredSet3));
            Assert.That(excludedRetired.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
        });
    }

    [Test]
    public void CodeEntrySelectionUsesTheDocumentedPrecedenceLevels()
    {
        CodeIndexEntry active = CreateCodeEntry("abc", 0, LanguageCodeForm.Set3);
        CodeIndexEntry alias = CreateCodeEntry("abc", 1, LanguageCodeForm.IanaLanguage);
        CodeIndexEntry retired = CreateCodeEntry("abc", 2, LanguageCodeForm.RetiredSet3);

        CodeIndexEntry[] activeFirst = LanguageLookupEngine.SelectPreferredCodeEntries(
            new[] { retired, alias, active },
            true);
        CodeIndexEntry[] aliasFirst = LanguageLookupEngine.SelectPreferredCodeEntries(
            new[] { retired, alias },
            true);
        CodeIndexEntry[] retiredIncluded = LanguageLookupEngine.SelectPreferredCodeEntries(
            new[] { retired },
            true);
        CodeIndexEntry[] retiredExcluded = LanguageLookupEngine.SelectPreferredCodeEntries(
            new[] { retired },
            false);

        Assert.Multiple(() =>
        {
            Assert.That(activeFirst, Is.EqualTo(new[] { active }));
            Assert.That(aliasFirst, Is.EqualTo(new[] { alias }));
            Assert.That(retiredIncluded, Is.EqualTo(new[] { retired }));
            Assert.That(retiredExcluded, Is.Empty);
        });
    }

    [Test]
    public void ResolverPrefersLiteralNamesBeforeNormalizedCollisions()
    {
        LanguageRow[] languages =
        {
            CreateRow("set3:aaa", "aaa"),
            CreateRow("set3:bbb", "bbb")
        };
        NameRow[] names =
        {
            CreateName("set3:aaa", "Shared-name"),
            CreateName("set3:bbb", "Shared name")
        };
        Dictionary<string, NameIndexEntry[]> index = NameIndexBuilder.BuildIndex(languages, names);
        LanguageCatalogData catalog = BuildCatalog(languages, names);
        LanguageLookupOptions options = CreateOptions(true, true, true, false, "EN");

        LanguageLookupResult literal = LanguageLookupEngine.ResolveNameValueEntries(
            "Shared name",
            options,
            index,
            catalog);
        LanguageLookupResult normalized = LanguageLookupEngine.ResolveNameValueEntries(
            "Shared name!",
            options,
            index,
            catalog);

        Assert.Multiple(() =>
        {
            Assert.That(literal.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(literal.Entry?.Set3Code, Is.EqualTo("bbb"));
            Assert.That(literal.Candidates, Has.Length.EqualTo(1));
            Assert.That(literal.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.ExactName));
            Assert.That(normalized.Status, Is.EqualTo(LanguageLookupStatus.Ambiguous));
            Assert.That(normalized.Entry, Is.Null);
            Assert.That(
                normalized.Candidates.Select(candidate => candidate.Entry?.Set3Code),
                Is.EqualTo(new[] { "aaa", "bbb" }));
            Assert.That(
                normalized.Candidates.All(
                    candidate => candidate.Kind == LanguageMatchKind.NormalizedName),
                Is.True);
        });
    }

    [Test]
    public void ResolverDistinguishesInvalidReservedAndUnknownValues()
    {
        LanguageLookupOptions options = CreateOptions(false, true, true, false, null);

        LanguageLookupResult nullValue = LanguageLookupEngine.ResolveValue(null, options);
        LanguageLookupResult malformedCode = LanguageLookupEngine.ResolveValue("e1g", options);
        LanguageLookupResult reserved = LanguageLookupEngine.ResolveValue("qaa", options);
        LanguageLookupResult unknownCode = LanguageLookupEngine.ResolveValue("zz", options);
        LanguageLookupResult unknownName = LanguageLookupEngine.ResolveValue(
            "Definitely Not A Registered Language Name",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(nullValue.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(malformedCode.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(reserved.Status, Is.EqualTo(LanguageLookupStatus.Reserved));
            Assert.That(unknownCode.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(unknownName.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(unknownName.NormalizedQuery, Is.EqualTo(
                "DEFINITELY NOT A REGISTERED LANGUAGE NAME"));
        });
    }

    [Test]
    public void RequireCodeReturnsFoundAndIncludedRetiredEntries()
    {
        LanguageCodeEntry active = LanguageLookupEngine.RequireCode(
            "eng",
            LanguageCodeForm.Set3,
            CreateOptions(false, false, false, false, null));
        LanguageCodeEntry retired = LanguageLookupEngine.RequireCode(
            "fri",
            LanguageCodeForm.RetiredSet3,
            CreateOptions(false, false, false, true, null));

        Assert.Multiple(() =>
        {
            Assert.That(active.Set3Code, Is.EqualTo("eng"));
            Assert.That(active.Status, Is.EqualTo(LanguageCodeStatus.Active));
            Assert.That(retired.Set3Code, Is.EqualTo("fri"));
            Assert.That(retired.Status, Is.EqualTo(LanguageCodeStatus.Retired));
            Assert.That(retired.Retirement, Is.Not.Null);
        });
    }

    [Test]
    public void RequireOperationsThrowOutcomeSpecificExceptions()
    {
        LanguageLookupOptions options = CreateOptions(false, true, true, false, null);
        LanguageLookupResult ambiguous = new LanguageLookupResult
        {
            Status = LanguageLookupStatus.Ambiguous
        };

        InvalidLanguageCodeException? invalidCode = Assert.Throws<InvalidLanguageCodeException>(
            () => LanguageLookupEngine.RequireCode(
                "e1g",
                LanguageCodeForm.Set3,
                options));
        LanguageCodeNotFoundException? missingCode = Assert.Throws<LanguageCodeNotFoundException>(
            () => LanguageLookupEngine.RequireCode(
                "zzz",
                LanguageCodeForm.Set3,
                options));
        InvalidLanguageCodeException? invalidValue = Assert.Throws<InvalidLanguageCodeException>(
            () => LanguageLookupEngine.RequireValue("e1g", options));
        LanguageCodeNotFoundException? missingValue =
            Assert.Throws<LanguageCodeNotFoundException>(
                () => LanguageLookupEngine.RequireValue("Not A Registered Language", options));
        AmbiguousLanguageNameException? ambiguousValue =
            Assert.Throws<AmbiguousLanguageNameException>(
                () => LanguageLookupEngine.RequireValueResult(ambiguous, "Shared name"));

        Assert.Multiple(() =>
        {
            Assert.That(invalidCode?.Message, Does.Contain("'e1g'"));
            Assert.That(missingCode?.Message, Does.Contain("'zzz'"));
            Assert.That(invalidValue?.Message, Does.Contain("'e1g'"));
            Assert.That(missingValue?.Message, Does.Contain("'Not A Registered Language'"));
            Assert.That(ambiguousValue?.Message, Does.Contain("'Shared name'"));
        });
    }

    [Test]
    public void RequiredEntriesAndResolverResultsAreDefensiveSnapshots()
    {
        LanguageLookupOptions options = CreateOptions(false, false, false, false, null);
        LanguageCodeEntry required = LanguageLookupEngine.RequireValue("English", options);
        LanguageLookupResult first = LanguageLookupEngine.ResolveValue("English", options);
        LanguageLookupResult second = LanguageLookupEngine.ResolveValue("English", options);

        required.Set3Code = "changed";
        first.Entry!.Names[0].Text = "changed";
        first.Candidates[0].Entry!.Set3Code = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(second.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(second.Entry?.Names[0].Text, Is.Not.EqualTo("changed"));
            Assert.That(second.Candidates[0].Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(LanguageLookupEngine.RequireValue("English", options).Set3Code, Is.EqualTo("eng"));
        });
    }

    private static LanguageLookupOptions CreateOptions(
        bool normalizeCase,
        bool normalizeNames,
        bool trimWhitespace,
        bool includeRetired,
        string? nameLanguage)
    {
        return new LanguageLookupOptions
        {
            NormalizeCase = normalizeCase,
            NormalizeNames = normalizeNames,
            TrimWhitespace = trimWhitespace,
            IncludeRetiredCodes = includeRetired,
            NameLanguage = nameLanguage
        };
    }

    private static CodeIndexEntry CreateCodeEntry(
        string value,
        int entityIndex,
        LanguageCodeForm form)
    {
        return new CodeIndexEntry(
            value,
            entityIndex,
            form,
            Iso639Set.Set3,
            form == LanguageCodeForm.RetiredSet3
                ? LanguageCodeStatus.Retired
                : LanguageCodeStatus.Active,
            null);
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
