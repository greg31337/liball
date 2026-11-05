using languagecodes;

namespace languagecodes.Tests;

public sealed class LanguageExceptionCoverageTests
{
    [TestCase("e1g", "Language code is invalid or ambiguous: 'e1g'")]
    [TestCase(null, "Language code is invalid or ambiguous: <null>")]
    public void RequireCodeInvalidInputsThrowExactExceptionWithContext(
        string? code,
        string expectedMessage)
    {
        LanguageCodeService service = new LanguageCodeService();

        InvalidLanguageCodeException? exception =
            Assert.Throws<InvalidLanguageCodeException>(() => service.RequireCode(
                code,
                LanguageCodeForm.Set3,
                new LanguageLookupOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception?.GetType(), Is.EqualTo(
                typeof(InvalidLanguageCodeException)));
            Assert.That(exception?.Message, Is.EqualTo(expectedMessage));
            Assert.That(exception?.InnerException, Is.Null);
        });
    }

    [TestCase(
        "zzz",
        LanguageCodeForm.Set3,
        false,
        "Language code was not found: 'zzz'")]
    [TestCase(
        "qaa",
        LanguageCodeForm.PrivateUse,
        false,
        "Language code was not found: 'qaa'")]
    [TestCase(
        "fri",
        LanguageCodeForm.RetiredSet3,
        false,
        "Language code was not found: 'fri'")]
    public void RequireCodeMissingOutcomesThrowNotFoundWithInputContext(
        string code,
        LanguageCodeForm form,
        bool includeRetired,
        string expectedMessage)
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions options = new LanguageLookupOptions
        {
            IncludeRetiredCodes = includeRetired
        };

        LanguageCodeNotFoundException? exception =
            Assert.Throws<LanguageCodeNotFoundException>(() => service.RequireCode(
                code,
                form,
                options));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception?.GetType(), Is.EqualTo(
                typeof(LanguageCodeNotFoundException)));
            Assert.That(exception?.Message, Is.EqualTo(expectedMessage));
            Assert.That(exception?.InnerException, Is.Null);
        });
    }

    [Test]
    public void RequireValueThrowsOutcomeSpecificExceptionsAndMessages()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions options = new LanguageLookupOptions
        {
            NormalizeCase = true
        };

        InvalidLanguageCodeException? invalid =
            Assert.Throws<InvalidLanguageCodeException>(() =>
                service.RequireValue("e1g", options));
        InvalidLanguageCodeException? nullInput =
            Assert.Throws<InvalidLanguageCodeException>(() =>
                service.RequireValue(null, options));
        LanguageCodeNotFoundException? missing =
            Assert.Throws<LanguageCodeNotFoundException>(() =>
                service.RequireValue("Not A Registered Language", options));
        AmbiguousLanguageNameException? ambiguous =
            Assert.Throws<AmbiguousLanguageNameException>(() =>
                service.RequireValue("MONGOL", options));

        Assert.Multiple(() =>
        {
            Assert.That(invalid?.GetType(), Is.EqualTo(
                typeof(InvalidLanguageCodeException)));
            Assert.That(invalid?.Message, Is.EqualTo(
                "Language value is invalid: 'e1g'"));
            Assert.That(nullInput?.GetType(), Is.EqualTo(
                typeof(InvalidLanguageCodeException)));
            Assert.That(nullInput?.Message, Is.EqualTo(
                "Language value is invalid: <null>"));
            Assert.That(missing?.GetType(), Is.EqualTo(
                typeof(LanguageCodeNotFoundException)));
            Assert.That(missing?.Message, Is.EqualTo(
                "Language value was not found: 'Not A Registered Language'"));
            Assert.That(ambiguous?.GetType(), Is.EqualTo(
                typeof(AmbiguousLanguageNameException)));
            Assert.That(ambiguous?.Message, Is.EqualTo(
                "Language value is ambiguous: 'MONGOL'"));
            Assert.That(invalid?.InnerException, Is.Null);
            Assert.That(nullInput?.InnerException, Is.Null);
            Assert.That(missing?.InnerException, Is.Null);
            Assert.That(ambiguous?.InnerException, Is.Null);
        });
    }

    [TestCase(null, "A language tag is required.")]
    [TestCase("", "A language tag is required.")]
    [TestCase("en--US", "Invalid RFC 5646 language tag: en--US")]
    [TestCase("en-", "Invalid RFC 5646 language tag: en-")]
    [TestCase("en-\u00e9\u00e9", "Invalid RFC 5646 language tag: en-\u00e9\u00e9")]
    public void ParseTagThrowsExactInvalidTagExceptionAndMessage(
        string? tag,
        string expectedMessage)
    {
        LanguageCodeService service = new LanguageCodeService();

        InvalidLanguageTagException? exception =
            Assert.Throws<InvalidLanguageTagException>(() => service.ParseTag(tag));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception?.GetType(), Is.EqualTo(
                typeof(InvalidLanguageTagException)));
            Assert.That(exception?.Message, Is.EqualTo(expectedMessage));
            Assert.That(exception?.InnerException, Is.Null);
        });
    }

    [Test]
    public void ResultReturningAlternativesPreserveExpectedFailuresWithoutThrowing()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions options = new LanguageLookupOptions
        {
            NormalizeCase = true
        };

        LanguageLookupResult invalidCode = service.LookupCode(
            "e1g",
            LanguageCodeForm.Set3,
            options);
        LanguageLookupResult missingCode = service.LookupCode(
            "zzz",
            LanguageCodeForm.Set3,
            options);
        LanguageLookupResult reservedCode = service.LookupCode(
            "qaa",
            LanguageCodeForm.PrivateUse,
            options);
        LanguageLookupResult invalidValue = service.ResolveValue("e1g", options);
        LanguageLookupResult missingValue = service.ResolveValue(
            "Not A Registered Language",
            options);
        LanguageLookupResult ambiguousValue = service.ResolveValue("MONGOL", options);
        LanguageValidationResult invalidTag = service.ValidateTag(
            "en--US",
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(invalidCode.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(invalidCode.NormalizedQuery, Is.EqualTo("e1g"));
            Assert.That(missingCode.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(missingCode.NormalizedQuery, Is.EqualTo("zzz"));
            Assert.That(reservedCode.Status, Is.EqualTo(LanguageLookupStatus.Reserved));
            Assert.That(reservedCode.NormalizedQuery, Is.EqualTo("qaa"));
            Assert.That(invalidValue.Status, Is.EqualTo(LanguageLookupStatus.Invalid));
            Assert.That(missingValue.Status, Is.EqualTo(LanguageLookupStatus.NotFound));
            Assert.That(ambiguousValue.Status, Is.EqualTo(LanguageLookupStatus.Ambiguous));
            Assert.That(ambiguousValue.Candidates, Has.Length.GreaterThan(1));
            Assert.That(invalidTag.IsWellFormed, Is.False);
            Assert.That(invalidTag.IsValid, Is.False);
            Assert.That(invalidTag.Issues, Is.Not.Empty);
        });
    }

    [Test]
    public void ThrowingOperationsReturnEntriesAndTagsOnSuccess()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions options = new LanguageLookupOptions
        {
            IncludeRetiredCodes = true
        };

        LanguageCodeEntry code = service.RequireCode(
            "eng",
            LanguageCodeForm.Set3,
            options);
        LanguageCodeEntry retired = service.RequireCode(
            "fri",
            LanguageCodeForm.RetiredSet3,
            options);
        LanguageCodeEntry value = service.RequireValue(
            "English",
            new LanguageLookupOptions());
        LanguageTag tag = service.ParseTag("en-US");

        Assert.Multiple(() =>
        {
            Assert.That(code.Set3Code, Is.EqualTo("eng"));
            Assert.That(retired.Set3Code, Is.EqualTo("fri"));
            Assert.That(retired.Status, Is.EqualTo(LanguageCodeStatus.Retired));
            Assert.That(value.Set3Code, Is.EqualTo("eng"));
            Assert.That(tag.NormalizedValue, Is.EqualTo("en-US"));
        });
    }
}
