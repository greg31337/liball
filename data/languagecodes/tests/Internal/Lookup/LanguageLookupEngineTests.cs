using languagecodes;

namespace languagecodes.Tests;

public class LanguageLookupEngineTests
{
    [TestCase("en", LanguageCodeForm.Alpha2, LanguageMatchKind.Alpha2, "eng")]
    [TestCase("fra", LanguageCodeForm.Set2Terminology, LanguageMatchKind.Set2Terminology, "fra")]
    [TestCase("fre", LanguageCodeForm.Set2Bibliographic, LanguageMatchKind.Set2Bibliographic, "fra")]
    [TestCase("fra", LanguageCodeForm.Set3, LanguageMatchKind.Set3, "fra")]
    [TestCase("afa", LanguageCodeForm.Set5, LanguageMatchKind.Set5, null)]
    [TestCase("iw", LanguageCodeForm.IanaLanguage, LanguageMatchKind.IanaAlias, "heb")]
    [TestCase("aao", LanguageCodeForm.IanaExtlang, LanguageMatchKind.IanaAlias, "aao")]
    [TestCase("i-klingon", LanguageCodeForm.GrandfatheredTag, LanguageMatchKind.IanaAlias, "tlh")]
    [TestCase("sgn-US", LanguageCodeForm.RedundantTag, LanguageMatchKind.IanaAlias, "ase")]
    public void ExplicitFormsResolveAndReportTheirMatchedForm(
        string code,
        LanguageCodeForm form,
        LanguageMatchKind kind,
        string? expectedSet3Code)
    {
        LanguageLookupResult result = LanguageLookupEngine.LookupCode(
            code,
            form,
            CreateOptions(false, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(result.Entry, Is.Not.Null);
            Assert.That(result.Entry?.Set3Code, Is.EqualTo(expectedSet3Code));
            Assert.That(result.Candidates, Has.Length.EqualTo(1));
            Assert.That(result.Candidates[0].Kind, Is.EqualTo(kind));
            Assert.That(result.Candidates[0].MatchedValue, Is.EqualTo(code));
            Assert.That(result.Candidates[0].Rank, Is.Zero);
        });
    }

    [Test]
    public void GenericLookupRetainsEveryMatchingFormForOneEntity()
    {
        LanguageLookupResult result = LanguageLookupEngine.LookupCode(
            "fra",
            LanguageCodeForm.Unknown,
            CreateOptions(false, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo("fra"));
            Assert.That(
                result.Candidates.Select(candidate => candidate.Kind),
                Is.EqualTo(new[]
                {
                    LanguageMatchKind.Set2Terminology,
                    LanguageMatchKind.Set3
                }));
            Assert.That(
                result.Candidates.Select(candidate => candidate.Rank),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(
                result.Candidates.Select(candidate => candidate.Entry?.Set3Code).Distinct(),
                Is.EqualTo(new[] { "fra" }));
        });
    }

    [Test]
    public void TrimmingAndCaseNormalizationAreAppliedOnlyWhenEnabled()
    {
        LanguageLookupResult normalized = LanguageLookupEngine.LookupCode(
            "  ENG  ",
            LanguageCodeForm.Set3,
            CreateOptions(true, true, false));
        LanguageLookupResult noTrim = LanguageLookupEngine.LookupCode(
            "  ENG  ",
            LanguageCodeForm.Set3,
            CreateOptions(false, true, false));
        LanguageLookupResult noCase = LanguageLookupEngine.LookupCode(
            "ENG",
            LanguageCodeForm.Set3,
            CreateOptions(false, false, false));
        LanguageLookupResult completeTag = LanguageLookupEngine.LookupCode(
            "I-KLINGON",
            LanguageCodeForm.GrandfatheredTag,
            CreateOptions(false, true, false));

        Assert.Multiple(() =>
        {
            Assert.That(normalized.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(normalized.NormalizedQuery, Is.EqualTo("eng"));
            Assert.That(noTrim.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(noTrim.NormalizedQuery, Is.EqualTo("  eng  "));
            Assert.That(noCase.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(noCase.NormalizedQuery, Is.EqualTo("ENG"));
            Assert.That(completeTag.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(completeTag.Entry?.Set3Code, Is.EqualTo("tlh"));
            Assert.That(completeTag.NormalizedQuery, Is.EqualTo("i-klingon"));
        });
    }

    [Test]
    public void RetiredCodesRequireExplicitInclusionAndRetainHistory()
    {
        LanguageLookupResult excluded = LanguageLookupEngine.LookupCode(
            "agp",
            LanguageCodeForm.RetiredSet3,
            CreateOptions(false, false, false));
        LanguageLookupResult included = LanguageLookupEngine.LookupCode(
            "agp",
            LanguageCodeForm.RetiredSet3,
            CreateOptions(false, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(excluded.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(excluded.Entry, Is.Null);
            Assert.That(excluded.Candidates, Is.Empty);
            Assert.That(included.Status, Is.EqualTo(LanguageLookupStatus.Retired));
            Assert.That(included.Entry?.Set3Code, Is.EqualTo("agp"));
            Assert.That(included.Entry?.Status, Is.EqualTo(LanguageCodeStatus.Retired));
            Assert.That(
                included.Entry?.Retirement?.ReplacementCodes,
                Is.EqualTo(new[] { "apf", "prf" }));
            Assert.That(included.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.RetiredSet3));
        });
    }

    [Test]
    public void PrivateUseCodesReturnReservedOutcomesAtBothBoundaries()
    {
        LanguageLookupResult first = LanguageLookupEngine.LookupCode(
            "qaa",
            LanguageCodeForm.PrivateUse,
            CreateOptions(false, false, false));
        LanguageLookupResult last = LanguageLookupEngine.LookupCode(
            "qtz",
            LanguageCodeForm.Unknown,
            CreateOptions(false, false, false));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(LanguageLookupStatus.Reserved));
            Assert.That(first.Entry, Is.Null);
            Assert.That(first.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.PrivateUse));
            Assert.That(last.Status, Is.EqualTo(LanguageLookupStatus.Reserved));
            Assert.That(last.Candidates[0].MatchedValue, Is.EqualTo("qtz"));
        });
    }

    [Test]
    public void InvalidShapesAndUnknownAssignmentsRemainDistinct()
    {
        LanguageLookupResult nullCode = LanguageLookupEngine.LookupCode(
            null,
            LanguageCodeForm.Unknown,
            CreateOptions(false, false, false));
        LanguageLookupResult empty = LanguageLookupEngine.LookupCode(
            string.Empty,
            LanguageCodeForm.Unknown,
            CreateOptions(false, false, false));
        LanguageLookupResult wrongLength = LanguageLookupEngine.LookupCode(
            "eng",
            LanguageCodeForm.Alpha2,
            CreateOptions(false, false, false));
        LanguageLookupResult invalidCharacters = LanguageLookupEngine.LookupCode(
            "e1g",
            LanguageCodeForm.Set3,
            CreateOptions(false, false, false));
        LanguageLookupResult unknown = LanguageLookupEngine.LookupCode(
            "abcd",
            LanguageCodeForm.Unknown,
            CreateOptions(false, false, false));

        Assert.Multiple(() =>
        {
            Assert.That(nullCode.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(nullCode.NormalizedQuery, Is.Null);
            Assert.That(empty.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(wrongLength.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(invalidCharacters.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(unknown.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(unknown.Candidates, Is.Empty);
        });
    }

    [Test]
    public void MultipleResolvedEntitiesProduceAnAmbiguousResult()
    {
        LanguageCatalogData catalog = LanguageCatalogBuilder.BuildCatalog(
            new[] { CreateRow("set3:aaa", "aaa"), CreateRow("set3:bbb", "bbb") },
            Array.Empty<NameRow>(),
            Array.Empty<AliasRow>(),
            Array.Empty<RelationshipRow>(),
            Array.Empty<RetirementRow>());
        CodeIndexEntry[] entries =
        {
            new CodeIndexEntry(
                "abc",
                0,
                LanguageCodeForm.Set3,
                Iso639Set.Set3,
                LanguageCodeStatus.Active,
                null),
            new CodeIndexEntry(
                "abc",
                1,
                LanguageCodeForm.IanaLanguage,
                Iso639Set.None,
                LanguageCodeStatus.Active,
                null)
        };

        LanguageLookupResult result = LanguageLookupEngine.BuildCodeLookupResult(
            "abc",
            entries,
            LanguageCodeForm.Unknown,
            false,
            catalog);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageLookupStatus.Ambiguous));
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.Candidates, Has.Length.EqualTo(2));
            Assert.That(
                result.Candidates.Select(candidate => candidate.Entry?.Set3Code),
                Is.EqualTo(new[] { "aaa", "bbb" }));
        });
    }

    [Test]
    public void LookupResultsCannotMutateCatalogOrLaterLookups()
    {
        LanguageLookupOptions options = CreateOptions(false, false, false);
        LanguageLookupResult first = LanguageLookupEngine.LookupCode(
            "aao",
            LanguageCodeForm.Set3,
            options);
        LanguageLookupResult second = LanguageLookupEngine.LookupCode(
            "aao",
            LanguageCodeForm.Set3,
            options);
        string originalName = second.Entry!.Names[0].Text!;

        first.Entry!.Set3Code = "changed";
        first.Entry.Names[0].Text = "changed";
        first.Candidates[0].Entry!.Set3Code = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first.Entry, Is.Not.SameAs(second.Entry));
            Assert.That(first.Candidates[0], Is.Not.SameAs(second.Candidates[0]));
            Assert.That(second.Entry?.Set3Code, Is.EqualTo("aao"));
            Assert.That(second.Entry?.Names[0].Text, Is.EqualTo(originalName));
            Assert.That(second.Candidates[0].Entry?.Set3Code, Is.EqualTo("aao"));
            Assert.That(
                LanguageLookupEngine.LookupCode("aao", LanguageCodeForm.Set3, options)
                    .Entry?.Set3Code,
                Is.EqualTo("aao"));
        });
    }

    private static LanguageLookupOptions CreateOptions(
        bool trimWhitespace,
        bool normalizeCase,
        bool includeRetired)
    {
        return new LanguageLookupOptions
        {
            TrimWhitespace = trimWhitespace,
            NormalizeCase = normalizeCase,
            IncludeRetiredCodes = includeRetired
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
}
