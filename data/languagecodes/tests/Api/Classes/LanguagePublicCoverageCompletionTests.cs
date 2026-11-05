using languagecodes;

namespace languagecodes.Tests;

public sealed class LanguagePublicCoverageCompletionTests
{
    [Test]
    public void NormalizedPunctuationOnlyValuesReturnInvalidOutcomes()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions options = new LanguageLookupOptions
        {
            NormalizeNames = true
        };

        LanguageLookupResult result = service.ResolveValue("!!!", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(result.NormalizedQuery, Is.EqualTo(string.Empty));
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.Candidates, Is.Empty);
        });
    }

    [Test]
    public void UndefinedFormsAndRepeatedTagSeparatorsAreInvalidLookupInputs()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions options = new LanguageLookupOptions();

        LanguageLookupResult undefinedForm = service.LookupCode(
            "eng",
            (LanguageCodeForm)int.MaxValue,
            options);
        LanguageLookupResult repeatedSeparator = service.LookupCode(
            "en--US",
            LanguageCodeForm.Unknown,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(undefinedForm.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(repeatedSeparator.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
        });
    }

    [Test]
    public void PrivateUseSpanCanBeCanonicalWithoutCatalogEntry()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageValidationOptions options = new LanguageValidationOptions
        {
            AllowPrivateUseCodes = true,
            RequireCanonical = true
        };

        bool valid = service.ValidateCodeSpan("qaa".AsSpan(), options);

        Assert.That(valid, Is.True);
    }

    [Test]
    public void LookupTruncationHandlesLeadingSingletonRange()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageTagMatchResult result = service.LookupBestTag(
            "a-en",
            Array.Empty<string>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageTagMatchStatus.NoMatch));
            Assert.That(result.NormalizedRange, Is.EqualTo("a-en"));
            Assert.That(result.MatchedIndex, Is.EqualTo(-1));
        });
    }

    [Test]
    public void UnassignedSpanAndEmptyNormalizedTagSnapshotReturnTypedFailures()
    {
        LanguageCodeService service = new LanguageCodeService();
        bool span = service.ValidateCodeSpan(
            "zzz".AsSpan(),
            new LanguageValidationOptions());
        LanguageLookupResult tag = service.ResolveTagLanguage(
            new LanguageTag { NormalizedValue = string.Empty });

        Assert.Multiple(() =>
        {
            Assert.That(span, Is.False);
            Assert.That(tag.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(tag.NormalizedQuery, Is.EqualTo(string.Empty));
        });
    }
}
