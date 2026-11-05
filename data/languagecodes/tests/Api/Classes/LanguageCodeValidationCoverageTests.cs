using languagecodes;

namespace languagecodes.Tests;

public sealed class LanguageCodeValidationCoverageTests
{
    [TestCase(null, LanguageValidationError.NullInput)]
    [TestCase("", LanguageValidationError.EmptyInput)]
    [TestCase("e", LanguageValidationError.InvalidLength)]
    [TestCase("\u00e9n", LanguageValidationError.NonAscii)]
    [TestCase("e1", LanguageValidationError.InvalidCharacter)]
    [TestCase("en-", LanguageValidationError.InvalidSeparator)]
    public void EveryLexicalFailureHasItsSpecificPublicDiagnostic(
        string? code,
        LanguageValidationError expectedError)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageValidationResult result = service.ValidateCode(
            code,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.False);
            Assert.That(result.IsAssigned, Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.CanonicalValue, Is.Null);
            Assert.That(result.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { expectedError }));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Error));
            Assert.That(result.Issues[0].Component, Is.EqualTo(code));
        });
    }

    [Test]
    public void RequiredSetAndFormConstraintsApplyToTheLiteralCode()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageValidationResult alpha2Set1 = service.ValidateCode(
            "en",
            new LanguageValidationOptions { RequiredSet = Iso639Set.Set1 });
        LanguageValidationResult alpha3NotSet1 = service.ValidateCode(
            "eng",
            new LanguageValidationOptions { RequiredSet = Iso639Set.Set1 });
        LanguageValidationResult sharedSet2 = service.ValidateCode(
            "fra",
            new LanguageValidationOptions { RequiredSet = Iso639Set.Set2 });
        LanguageValidationResult sharedSet3 = service.ValidateCode(
            "fra",
            new LanguageValidationOptions { RequiredSet = Iso639Set.Set3 });
        LanguageValidationResult bibliographic = service.ValidateCode(
            "fre",
            new LanguageValidationOptions
            {
                RequiredForm = LanguageCodeForm.Set2Bibliographic
            });
        LanguageValidationResult bibliographicNotSet3 = service.ValidateCode(
            "fre",
            new LanguageValidationOptions { RequiredForm = LanguageCodeForm.Set3 });
        LanguageValidationResult set5 = service.ValidateCode(
            "gmw",
            new LanguageValidationOptions
            {
                RequiredSet = Iso639Set.Set5,
                RequiredForm = LanguageCodeForm.Set5,
                AllowGroupCodes = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(alpha2Set1.IsValid, Is.True);
            Assert.That(alpha3NotSet1.IsValid, Is.False);
            Assert.That(alpha3NotSet1.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.WrongSet));
            Assert.That(sharedSet2.IsValid, Is.True);
            Assert.That(sharedSet3.IsValid, Is.True);
            Assert.That(bibliographic.IsValid, Is.True);
            Assert.That(bibliographicNotSet3.IsValid, Is.False);
            Assert.That(bibliographicNotSet3.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.WrongCodeForm));
            Assert.That(set5.IsValid, Is.True);
            Assert.That(set5.Entry?.Set5Code, Is.EqualTo("gmw"));
        });
    }

    [Test]
    public void LifecycleAndEntityPoliciesCoverEveryCodeCategory()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageValidationOptions activeOnly = new LanguageValidationOptions
        {
            AllowedStatuses = new[] { LanguageCodeStatus.Active }
        };

        LanguageValidationResult individual = service.ValidateCode(
            "en",
            new LanguageValidationOptions());
        LanguageValidationResult retiredDefault = service.ValidateCode(
            "fri",
            new LanguageValidationOptions());
        LanguageValidationResult retiredRejected = service.ValidateCode(
            "fri",
            activeOnly);
        LanguageValidationResult retiredAccepted = service.ValidateCode(
            "fri",
            new LanguageValidationOptions
            {
                AllowedStatuses = new[] { LanguageCodeStatus.Retired }
            });
        LanguageValidationResult aliasDefault = service.ValidateCode(
            "iw",
            new LanguageValidationOptions());
        LanguageValidationResult aliasRejected = service.ValidateCode(
            "iw",
            activeOnly);
        LanguageValidationResult specialDefault = service.ValidateCode(
            "mis",
            new LanguageValidationOptions());
        LanguageValidationResult specialAccepted = service.ValidateCode(
            "mis",
            new LanguageValidationOptions { AllowSpecialCodes = true });
        LanguageValidationResult groupDefault = service.ValidateCode(
            "afa",
            new LanguageValidationOptions());
        LanguageValidationResult groupAccepted = service.ValidateCode(
            "afa",
            new LanguageValidationOptions { AllowGroupCodes = true });
        LanguageValidationResult macroDefault = service.ValidateCode(
            "ara",
            new LanguageValidationOptions());
        LanguageValidationResult macroAccepted = service.ValidateCode(
            "ara",
            new LanguageValidationOptions { AllowMacrolanguages = true });

        Assert.Multiple(() =>
        {
            Assert.That(individual.IsValid, Is.True);
            Assert.That(individual.Entry?.Scope, Is.EqualTo(LanguageScope.Individual));
            Assert.That(retiredDefault.IsValid, Is.True);
            Assert.That(retiredDefault.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.RetiredCode));
            Assert.That(retiredDefault.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Warning));
            Assert.That(retiredRejected.IsValid, Is.False);
            Assert.That(retiredRejected.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Error));
            Assert.That(retiredAccepted.IsValid, Is.True);
            Assert.That(aliasDefault.IsValid, Is.True);
            Assert.That(aliasDefault.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.DeprecatedCode));
            Assert.That(aliasDefault.CanonicalValue, Is.EqualTo("he"));
            Assert.That(aliasRejected.IsValid, Is.False);
            Assert.That(specialDefault.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.SpecialCodeNotAllowed));
            Assert.That(specialAccepted.IsValid, Is.True);
            Assert.That(groupDefault.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.GroupCodeNotAllowed));
            Assert.That(groupAccepted.IsValid, Is.True);
            Assert.That(macroDefault.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.DisallowedScope));
            Assert.That(macroAccepted.IsValid, Is.True);
        });
    }

    [Test]
    public void PrivateUseEndpointsAreReservedAndTheNextPrefixIsUnassigned()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageValidationOptions options = new LanguageValidationOptions
        {
            RequiredSet = Iso639Set.Set3,
            RequiredForm = LanguageCodeForm.PrivateUse,
            AllowedStatuses = new[] { LanguageCodeStatus.PrivateUse },
            AllowPrivateUseCodes = true
        };

        LanguageValidationResult first = service.ValidateCode("qaa", options);
        LanguageValidationResult last = service.ValidateCode("qtz", options);
        LanguageValidationResult outside = service.ValidateCode(
            "qzz",
            new LanguageValidationOptions { AllowPrivateUseCodes = true });

        Assert.Multiple(() =>
        {
            AssertReservedPrivateUse(first, "qaa");
            AssertReservedPrivateUse(last, "qtz");
            Assert.That(outside.IsWellFormed, Is.True);
            Assert.That(outside.IsAssigned, Is.False);
            Assert.That(outside.IsValid, Is.False);
            Assert.That(outside.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.UnassignedCode }));
        });
    }

    [Test]
    public void CanonicalAlternativesWarnOrFailAccordingToPolicy()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageValidationResult warning = service.ValidateCode(
            "fre",
            new LanguageValidationOptions());
        LanguageValidationResult strict = service.ValidateCode(
            "fre",
            new LanguageValidationOptions { RequireCanonical = true });
        LanguageValidationResult exactForm = service.ValidateCode(
            "fre",
            new LanguageValidationOptions
            {
                RequiredForm = LanguageCodeForm.Set2Bibliographic,
                RequireCanonical = true
            });
        LanguageValidationResult uppercase = service.ValidateCode(
            "AAA",
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(warning.IsValid, Is.True);
            Assert.That(warning.IsCanonical, Is.False);
            Assert.That(warning.CanonicalValue, Is.EqualTo("fra"));
            Assert.That(warning.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.NonCanonical));
            Assert.That(warning.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Warning));
            Assert.That(strict.IsValid, Is.False);
            Assert.That(strict.CanonicalValue, Is.EqualTo("fra"));
            Assert.That(strict.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Error));
            Assert.That(exactForm.IsValid, Is.True);
            Assert.That(exactForm.IsCanonical, Is.True);
            Assert.That(exactForm.CanonicalValue, Is.Null);
            Assert.That(uppercase.IsValid, Is.True);
            Assert.That(uppercase.IsCanonical, Is.False);
            Assert.That(uppercase.CanonicalValue, Is.EqualTo("aaa"));
            Assert.That(uppercase.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.NonCanonical));
        });
    }

    [TestCase("en", LanguageCodeForm.Unknown, true)]
    [TestCase("ENG", LanguageCodeForm.Set3, true)]
    [TestCase("fre", LanguageCodeForm.Set2Bibliographic, true)]
    [TestCase("iw", LanguageCodeForm.IanaLanguage, true)]
    [TestCase("fri", LanguageCodeForm.RetiredSet3, true)]
    [TestCase("qaa", LanguageCodeForm.Unknown, false)]
    [TestCase("zzz", LanguageCodeForm.Unknown, false)]
    [TestCase("eng", LanguageCodeForm.Alpha2, false)]
    [TestCase("en", LanguageCodeForm.Set3, false)]
    [TestCase(null, LanguageCodeForm.Unknown, false)]
    public void BooleanAssignmentUsesTheRequestedForm(
        string? code,
        LanguageCodeForm form,
        bool expected)
    {
        LanguageCodeService service = new LanguageCodeService();

        Assert.That(service.IsAssignedCode(code, form), Is.EqualTo(expected));
    }

    [Test]
    public void SpanValidationMatchesDetailedValidationAcrossPolicies()
    {
        LanguageCodeService service = new LanguageCodeService();
        string[] codes =
        {
            string.Empty,
            "e",
            "e1",
            "e-g",
            "\u00e9n",
            "EN",
            "eng",
            "fre",
            "iw",
            "fri",
            "aaa",
            "zzz",
            "ara",
            "afa",
            "mis",
            "qaa",
            "qtz",
            "qzz"
        };
        LanguageValidationOptions[] policies =
        {
            new LanguageValidationOptions(),
            new LanguageValidationOptions
            {
                AllowMacrolanguages = true,
                AllowGroupCodes = true,
                AllowSpecialCodes = true,
                AllowPrivateUseCodes = true
            },
            new LanguageValidationOptions
            {
                AllowedStatuses = new[] { LanguageCodeStatus.Active }
            },
            new LanguageValidationOptions
            {
                AllowedStatuses = new[] { LanguageCodeStatus.Retired }
            },
            new LanguageValidationOptions { RequiredSet = Iso639Set.Set2 },
            new LanguageValidationOptions
            {
                RequiredForm = LanguageCodeForm.Set2Bibliographic
            },
            new LanguageValidationOptions { RequireCanonical = true },
            new LanguageValidationOptions
            {
                RequiredSet = Iso639Set.Set3,
                RequiredForm = LanguageCodeForm.PrivateUse,
                AllowedStatuses = new[] { LanguageCodeStatus.PrivateUse },
                AllowPrivateUseCodes = true
            }
        };

        int policyIndex;
        for (policyIndex = 0; policyIndex < policies.Length; policyIndex++)
        {
            foreach (string code in codes)
            {
                bool detailed = service.ValidateCode(code, policies[policyIndex]).IsValid;
                bool span = service.ValidateCodeSpan(
                    code.AsSpan(),
                    policies[policyIndex]);

                Assert.That(
                    span,
                    Is.EqualTo(detailed),
                    $"Span validation differed for '{code}' under policy {policyIndex}.");
            }
        }
    }

    private static void AssertReservedPrivateUse(
        LanguageValidationResult result,
        string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True, code);
            Assert.That(result.IsAssigned, Is.False, code);
            Assert.That(result.IsValid, Is.True, code);
            Assert.That(result.IsCanonical, Is.True, code);
            Assert.That(result.Entry, Is.Null, code);
            Assert.That(result.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.ReservedCode }), code);
            Assert.That(result.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Warning), code);
        });
    }
}
