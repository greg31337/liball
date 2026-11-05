using languagecodes;

namespace languagecodes.Tests;

public class LanguageTagResolutionEngineTests
{
    [TestCase("en-US", "eng")]
    [TestCase("eng-US", "eng")]
    [TestCase("iw-IL", "heb")]
    public void ResolvesPrimaryLanguageFormsAndDeprecatedAliases(
        string value,
        string expectedSet3Code)
    {
        LanguageLookupResult result = LanguageLookupEngine.ResolveTagLanguage(
            LanguageTagParser.ParseTag(value));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo(expectedSet3Code));
            Assert.That(result.NormalizedQuery, Is.EqualTo(
                LanguageTagParser.ParseTag(value).Language));
            Assert.That(result.Candidates, Is.Not.Empty);
        });
    }

    [TestCase("ar-aao-DZ", "aao")]
    [TestCase("ar-ajp", "apc")]
    public void UsesTheRegisteredExtlangAsTheEffectiveLanguage(
        string value,
        string expectedSet3Code)
    {
        LanguageLookupResult result = LanguageLookupEngine.ResolveTagLanguage(
            LanguageTagParser.ParseTag(value));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo(expectedSet3Code));
            Assert.That(result.NormalizedQuery, Is.EqualTo(
                LanguageTagParser.ParseTag(value).Extlangs[0]));
        });
    }

    [Test]
    public void DeprecatedLanguageAndExtlangAliasesResolveToAuthoritativeEntries()
    {
        LanguageLookupResult language = LanguageLookupEngine.ResolveTagLanguage(
            LanguageTagParser.ParseTag("iw-IL"));
        LanguageLookupResult extlang = LanguageLookupEngine.ResolveTagLanguage(
            LanguageTagParser.ParseTag("ar-ajp"));

        Assert.Multiple(() =>
        {
            Assert.That(language.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(language.Entry?.Set3Code, Is.EqualTo("heb"));
            Assert.That(language.Candidates[0].Kind, Is.EqualTo(
                LanguageMatchKind.IanaAlias));
            Assert.That(language.Candidates[0].MatchedValue, Is.EqualTo("iw"));
            Assert.That(extlang.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(extlang.Entry?.Set3Code, Is.EqualTo("apc"));
            Assert.That(extlang.Candidates[0].Kind, Is.EqualTo(
                LanguageMatchKind.IanaAlias));
            Assert.That(extlang.Candidates[0].MatchedValue, Is.EqualTo("ajp"));
        });
    }

    [TestCase("en-aao", LanguageLookupStatus.Invalid, "aao")]
    [TestCase("en-zzz", LanguageLookupStatus.NotFound, "zzz")]
    [TestCase("zh-cmn-yue", LanguageLookupStatus.Invalid, "zh-cmn-yue")]
    public void DistinguishesInvalidAndUnassignedExtlangOutcomes(
        string value,
        LanguageLookupStatus expectedStatus,
        string expectedQuery)
    {
        LanguageLookupResult result = LanguageLookupEngine.ResolveTagLanguage(
            LanguageTagParser.ParseTag(value));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.NormalizedQuery, Is.EqualTo(expectedQuery));
            Assert.That(result.Candidates, Is.Empty);
        });
    }

    [TestCase("i-klingon", LanguageLookupStatus.Found, "tlh")]
    [TestCase("sgn-US", LanguageLookupStatus.Found, "ase")]
    [TestCase("de-CH-1901", LanguageLookupStatus.Found, "deu")]
    [TestCase("i-default", LanguageLookupStatus.NotFound, null)]
    public void ResolvesCompleteRegistryTagsBeforeFallingBackToTheirPrimaryLanguage(
        string value,
        LanguageLookupStatus expectedStatus,
        string? expectedSet3Code)
    {
        LanguageLookupResult result = LanguageLookupEngine.ResolveTagLanguage(
            LanguageTagParser.ParseTag(value));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo(expectedSet3Code));
        });
    }

    [Test]
    public void PrivateUseOutcomesDoNotInventLanguageEntities()
    {
        LanguageLookupResult privateOnly = LanguageLookupEngine.ResolveTagLanguage(
            LanguageTagParser.ParseTag("x-company-internal"));
        LanguageLookupResult privateLanguage = LanguageLookupEngine.ResolveTagLanguage(
            LanguageTagParser.ParseTag("qaa-US"));
        LanguageLookupResult privateSuffix = LanguageLookupEngine.ResolveTagLanguage(
            LanguageTagParser.ParseTag("en-US-x-company"));

        Assert.Multiple(() =>
        {
            Assert.That(privateOnly.Status, Is.EqualTo(LanguageLookupStatus.Reserved));
            Assert.That(privateOnly.Entry, Is.Null);
            Assert.That(privateOnly.NormalizedQuery, Is.EqualTo("x-company-internal"));
            Assert.That(privateLanguage.Status, Is.EqualTo(LanguageLookupStatus.Reserved));
            Assert.That(privateLanguage.Entry, Is.Null);
            Assert.That(privateLanguage.NormalizedQuery, Is.EqualTo("qaa"));
            Assert.That(privateLanguage.Candidates, Has.Length.EqualTo(1));
            Assert.That(privateLanguage.Candidates[0].Kind, Is.EqualTo(
                LanguageMatchKind.PrivateUse));
            Assert.That(privateSuffix.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(privateSuffix.Entry?.Set3Code, Is.EqualTo("eng"));
        });
    }

    [TestCase("abcde-US", LanguageLookupStatus.NotFound)]
    [TestCase("agp-US", LanguageLookupStatus.Retired)]
    public void PreservesUnresolvedAndRetiredIsoOutcomes(
        string value,
        LanguageLookupStatus expectedStatus)
    {
        LanguageLookupResult result = LanguageLookupEngine.ResolveTagLanguage(
            LanguageTagParser.ParseTag(value));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.NormalizedQuery, Is.EqualTo(
                LanguageTagParser.ParseTag(value).Language));
        });
    }

    [Test]
    public void RejectsMalformedOrIncompleteParsedSnapshots()
    {
        LanguageLookupResult malformed = LanguageLookupEngine.ResolveTagLanguage(
            new LanguageTag
            {
                Value = "en--US",
                Language = "en"
            });
        LanguageLookupResult incomplete = LanguageLookupEngine.ResolveTagLanguage(
            new LanguageTag
            {
                Language = "en"
            });
        LanguageLookupResult normalizedFallback =
            LanguageLookupEngine.ResolveTagLanguage(
                new LanguageTag
                {
                    NormalizedValue = "en-US"
                });

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(incomplete.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(normalizedFallback.Status, Is.EqualTo(LanguageLookupStatus.Found));
            Assert.That(normalizedFallback.Entry?.Set3Code, Is.EqualTo("eng"));
        });
    }

    [Test]
    public void ValidationAndNormalizationExposeTheResolvedLanguageEntry()
    {
        LanguageValidationResult validation = LanguageValidationEngine.ValidateTag(
            "en-US",
            new LanguageValidationOptions());
        LanguageNormalizationResult normalization = LanguageNormalizationEngine.NormalizeTag(
            "eng-US",
            new LanguageNormalizationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(validation.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(normalization.Output, Is.EqualTo("en-US"));
            Assert.That(normalization.Entry?.Set3Code, Is.EqualTo("eng"));
        });
    }

    [Test]
    public void ResolutionResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageTag tag = LanguageTagParser.ParseTag("en-US");
        LanguageLookupResult first = LanguageLookupEngine.ResolveTagLanguage(tag);
        LanguageLookupResult second = LanguageLookupEngine.ResolveTagLanguage(tag);
        string originalName = second.Entry!.Names[0].Text!;

        first.Entry!.Set3Code = "changed";
        first.Entry.Names[0].Text = "changed";
        first.Candidates[0].MatchedValue = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Entry, Is.Not.SameAs(second.Entry));
            Assert.That(first.Candidates, Is.Not.SameAs(second.Candidates));
            Assert.That(second.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(second.Entry?.Names[0].Text, Is.EqualTo(originalName));
            Assert.That(second.Candidates[0].MatchedValue, Is.EqualTo("en"));
        });
    }
}
