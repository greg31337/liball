using languagecodes;

namespace languagecodes.Tests;

public sealed class LanguageCodeLookupCoverageTests
{
    [TestCase("en", LanguageCodeForm.Alpha2, LanguageLookupStatus.Found, LanguageMatchKind.Alpha2, "eng", false)]
    [TestCase("fra", LanguageCodeForm.Set2Terminology, LanguageLookupStatus.Found, LanguageMatchKind.Set2Terminology, "fra", false)]
    [TestCase("fre", LanguageCodeForm.Set2Bibliographic, LanguageLookupStatus.Found, LanguageMatchKind.Set2Bibliographic, "fra", false)]
    [TestCase("fra", LanguageCodeForm.Set3, LanguageLookupStatus.Found, LanguageMatchKind.Set3, "fra", false)]
    [TestCase("afa", LanguageCodeForm.Set5, LanguageLookupStatus.Found, LanguageMatchKind.Set5, null, false)]
    [TestCase("agp", LanguageCodeForm.RetiredSet3, LanguageLookupStatus.Retired, LanguageMatchKind.RetiredSet3, "agp", true)]
    [TestCase("iw", LanguageCodeForm.IanaLanguage, LanguageLookupStatus.Found, LanguageMatchKind.IanaAlias, "heb", false)]
    [TestCase("aao", LanguageCodeForm.IanaExtlang, LanguageLookupStatus.Found, LanguageMatchKind.IanaAlias, "aao", false)]
    [TestCase("i-klingon", LanguageCodeForm.GrandfatheredTag, LanguageLookupStatus.Found, LanguageMatchKind.IanaAlias, "tlh", false)]
    [TestCase("sgn-US", LanguageCodeForm.RedundantTag, LanguageLookupStatus.Found, LanguageMatchKind.IanaAlias, "ase", false)]
    [TestCase("qaa", LanguageCodeForm.PrivateUse, LanguageLookupStatus.Reserved, LanguageMatchKind.PrivateUse, null, false)]
    public void LookupCodeSupportsEveryExplicitCodeForm(
        string code,
        LanguageCodeForm form,
        LanguageLookupStatus expectedStatus,
        LanguageMatchKind expectedKind,
        string? expectedSet3Code,
        bool includeRetired)
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions options = new LanguageLookupOptions
        {
            IncludeRetiredCodes = includeRetired
        };

        LanguageLookupResult result = service.LookupCode(code, form, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo(expectedSet3Code));
            Assert.That(result.Candidates, Has.Length.EqualTo(1));
            Assert.That(result.Candidates[0].Kind, Is.EqualTo(expectedKind));
            Assert.That(result.Candidates[0].Rank, Is.Zero);
            Assert.That(result.NormalizedQuery, Is.EqualTo(code));
        });
    }

    [Test]
    public void GenericLookupRetainsSameEntityCodeFormCollisions()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageLookupResult generic = service.LookupCode(
            "fra",
            LanguageCodeForm.Unknown,
            new LanguageLookupOptions());
        LanguageLookupResult wrongExplicitForm = service.LookupCode(
            "fra",
            LanguageCodeForm.Set2Bibliographic,
            new LanguageLookupOptions());

        Assert.Multiple(() =>
        {
            Assert.That(generic.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(generic.Entry?.Set3Code, Is.EqualTo("fra"));
            Assert.That(
                generic.Candidates.Select(candidate => candidate.Kind),
                Is.EqualTo(new[]
                {
                    LanguageMatchKind.Set2Terminology,
                    LanguageMatchKind.Set3
                }));
            Assert.That(
                generic.Candidates.Select(candidate => candidate.Entry?.Set3Code).Distinct(),
                Is.EqualTo(new[] { "fra" }));
            Assert.That(wrongExplicitForm.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(wrongExplicitForm.Candidates, Is.Empty);
        });
    }

    [TestCase("EN", LanguageCodeForm.Alpha2, "eng", false)]
    [TestCase("FRA", LanguageCodeForm.Set2Terminology, "fra", false)]
    [TestCase("FRE", LanguageCodeForm.Set2Bibliographic, "fra", false)]
    [TestCase("DEU", LanguageCodeForm.Set3, "deu", false)]
    [TestCase("AFA", LanguageCodeForm.Set5, null, false)]
    [TestCase("AGP", LanguageCodeForm.RetiredSet3, "agp", true)]
    [TestCase("IW", LanguageCodeForm.IanaLanguage, "heb", false)]
    [TestCase("AAO", LanguageCodeForm.IanaExtlang, "aao", false)]
    [TestCase("I-KLINGON", LanguageCodeForm.GrandfatheredTag, "tlh", false)]
    [TestCase("SGN-us", LanguageCodeForm.RedundantTag, "ase", false)]
    public void MixedCaseLookupRequiresCaseNormalization(
        string code,
        LanguageCodeForm form,
        string? expectedSet3Code,
        bool includeRetired)
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupResult normalized = service.LookupCode(
            code,
            form,
            new LanguageLookupOptions
            {
                NormalizeCase = true,
                IncludeRetiredCodes = includeRetired
            });
        LanguageLookupResult literal = service.LookupCode(
            code,
            form,
            new LanguageLookupOptions
            {
                NormalizeCase = false,
                IncludeRetiredCodes = includeRetired
            });

        Assert.Multiple(() =>
        {
            Assert.That(
                normalized.Status,
                Is.EqualTo(includeRetired ? LanguageLookupStatus.Retired : LanguageLookupStatus.Found));
            Assert.That(normalized.Entry?.Set3Code, Is.EqualTo(expectedSet3Code));
            Assert.That(literal.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
        });
    }

    [Test]
    public void WhitespaceIsTrimmedOnlyWhenRequested()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupResult trimmed = service.LookupCode(
            "  EN  ",
            LanguageCodeForm.Alpha2,
            new LanguageLookupOptions
            {
                TrimWhitespace = true,
                NormalizeCase = true
            });
        LanguageLookupResult literal = service.LookupCode(
            "  EN  ",
            LanguageCodeForm.Alpha2,
            new LanguageLookupOptions
            {
                TrimWhitespace = false,
                NormalizeCase = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(trimmed.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(trimmed.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(trimmed.NormalizedQuery, Is.EqualTo("en"));
            Assert.That(literal.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(literal.NormalizedQuery, Is.EqualTo("  en  "));
        });
    }

    [TestCase("mis")]
    [TestCase("mul")]
    [TestCase("und")]
    [TestCase("zxx")]
    public void SpecialIdentifiersResolveAsActiveSpecialEntities(string code)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageLookupResult explicitResult = service.LookupCode(
            code,
            LanguageCodeForm.Set3,
            new LanguageLookupOptions());
        LanguageLookupResult genericResult = service.LookupCode(
            code,
            LanguageCodeForm.Unknown,
            new LanguageLookupOptions());

        Assert.Multiple(() =>
        {
            Assert.That(explicitResult.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(explicitResult.Entry?.Set3Code, Is.EqualTo(code));
            Assert.That(explicitResult.Entry?.Scope, Is.EqualTo(LanguageScope.Special));
            Assert.That(explicitResult.Entry?.LanguageType, Is.EqualTo(LanguageType.Special));
            Assert.That(explicitResult.Entry?.Status, Is.EqualTo(LanguageCodeStatus.Active));
            Assert.That(genericResult.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(genericResult.Entry?.Set3Code, Is.EqualTo(code));
        });
    }

    [TestCase("qaa")]
    [TestCase("qab")]
    [TestCase("qtz")]
    public void PrivateUseRangeIncludesBoundariesAndInterior(string code)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageLookupResult explicitResult = service.LookupCode(
            code,
            LanguageCodeForm.PrivateUse,
            new LanguageLookupOptions());
        LanguageLookupResult genericResult = service.LookupCode(
            code,
            LanguageCodeForm.Unknown,
            new LanguageLookupOptions());

        Assert.Multiple(() =>
        {
            Assert.That(explicitResult.Status, Is.EqualTo(LanguageLookupStatus.Reserved));
            Assert.That(explicitResult.Entry, Is.Null);
            Assert.That(explicitResult.Candidates[0].Kind, Is.EqualTo(LanguageMatchKind.PrivateUse));
            Assert.That(genericResult.Status, Is.EqualTo(LanguageLookupStatus.Reserved));
            Assert.That(genericResult.Entry, Is.Null);
        });
    }

    [Test]
    public void UnknownAndOutsidePrivateUseCodesRemainNotFound()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageLookupResult unknownSet3 = service.LookupCode(
            "zzz",
            LanguageCodeForm.Set3,
            new LanguageLookupOptions());
        LanguageLookupResult outsidePrivateUse = service.LookupCode(
            "qzz",
            LanguageCodeForm.PrivateUse,
            new LanguageLookupOptions());

        Assert.Multiple(() =>
        {
            Assert.That(unknownSet3.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(unknownSet3.Entry, Is.Null);
            Assert.That(unknownSet3.Candidates, Is.Empty);
            Assert.That(outsidePrivateUse.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(outsidePrivateUse.Candidates, Is.Empty);
        });
    }

    [Test]
    public void RetiredLookupHonorsFormAndInclusionPolicy()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupResult included = service.LookupCode(
            "fri",
            LanguageCodeForm.RetiredSet3,
            new LanguageLookupOptions { IncludeRetiredCodes = true });
        LanguageLookupResult excluded = service.LookupCode(
            "fri",
            LanguageCodeForm.RetiredSet3,
            new LanguageLookupOptions { IncludeRetiredCodes = false });
        LanguageLookupResult activeForm = service.LookupCode(
            "fri",
            LanguageCodeForm.Set3,
            new LanguageLookupOptions { IncludeRetiredCodes = true });

        Assert.Multiple(() =>
        {
            Assert.That(included.Status, Is.EqualTo(LanguageLookupStatus.Retired));
            Assert.That(included.Entry?.Set3Code, Is.EqualTo("fri"));
            Assert.That(included.Entry?.Retirement?.ReplacementCodes, Is.EqualTo(new[] { "fry" }));
            Assert.That(excluded.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(activeForm.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
        });
    }

    [Test]
    public void RequireCodeReturnsActiveAndExplicitlyIncludedRetiredEntries()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageCodeEntry active = service.RequireCode(
            "ENG",
            LanguageCodeForm.Set3,
            new LanguageLookupOptions { NormalizeCase = true });
        LanguageCodeEntry retired = service.RequireCode(
            "fri",
            LanguageCodeForm.RetiredSet3,
            new LanguageLookupOptions { IncludeRetiredCodes = true });

        Assert.Multiple(() =>
        {
            Assert.That(active.Set3Code, Is.EqualTo("eng"));
            Assert.That(active.Status, Is.EqualTo(LanguageCodeStatus.Active));
            Assert.That(retired.Set3Code, Is.EqualTo("fri"));
            Assert.That(retired.Status, Is.EqualTo(LanguageCodeStatus.Retired));
            Assert.That(retired.Retirement?.ReplacementCodes, Is.EqualTo(new[] { "fry" }));
        });
    }

    [Test]
    public void RequireCodeThrowsForInvalidMissingReservedAndExcludedRetiredCodes()
    {
        LanguageCodeService service = new LanguageCodeService();

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidLanguageCodeException>(() => service.RequireCode(
                "e1g",
                LanguageCodeForm.Set3,
                new LanguageLookupOptions()));
            Assert.Throws<InvalidLanguageCodeException>(() => service.RequireCode(
                null,
                LanguageCodeForm.Unknown,
                new LanguageLookupOptions()));
            Assert.Throws<LanguageCodeNotFoundException>(() => service.RequireCode(
                "zzz",
                LanguageCodeForm.Set3,
                new LanguageLookupOptions()));
            Assert.Throws<LanguageCodeNotFoundException>(() => service.RequireCode(
                "qaa",
                LanguageCodeForm.PrivateUse,
                new LanguageLookupOptions()));
            Assert.Throws<LanguageCodeNotFoundException>(() => service.RequireCode(
                "fri",
                LanguageCodeForm.RetiredSet3,
                new LanguageLookupOptions { IncludeRetiredCodes = false }));
        });
    }
}
