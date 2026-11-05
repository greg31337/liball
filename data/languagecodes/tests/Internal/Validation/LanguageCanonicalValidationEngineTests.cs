using languagecodes;

namespace languagecodes.Tests;

public class LanguageCanonicalValidationEngineTests
{
    [Test]
    public void BibliographicCodesPreferTheirTerminologyEquivalent()
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCode(
                "fre",
                new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.Entry?.Set3Code, Is.EqualTo("fra"));
            Assert.That(result.CanonicalValue, Is.EqualTo("fra"));
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Warning));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.NonCanonical));
            Assert.That(result.Issues[0].Component, Is.EqualTo("fre"));
            Assert.That(result.Issues[0].Message, Is.EqualTo(
                "The terminology code fra is preferred."));
        });
    }

    [TestCase("eng", "en")]
    [TestCase("fra", "fr")]
    [TestCase("deu", "de")]
    public void TerminologyAndSet3CodesPreferTheShortestAlpha2Form(
        string code,
        string expectedCanonical)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCode(
                code,
                new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.CanonicalValue, Is.EqualTo(expectedCanonical));
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Warning));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.NonCanonical));
            Assert.That(result.Issues[0].Message, Is.EqualTo(
                $"The shorter alpha-2 code {expectedCanonical} is preferred."));
        });
    }

    [TestCase("en")]
    [TestCase("aaa")]
    public void AlreadyPreferredCodesHaveNoCanonicalityDiagnostic(string code)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCode(
                code,
                new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.True);
            Assert.That(result.CanonicalValue, Is.Null);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void RequiredSetAndFormPoliciesSuppressIncompatibleShortening()
    {
        LanguageValidationResult set2Terminology =
            LanguageValidationEngine.ValidateCode(
                "fra",
                new LanguageValidationOptions
                {
                    RequiredSet = Iso639Set.Set2
                });
        LanguageValidationResult set3 =
            LanguageValidationEngine.ValidateCode(
                "fra",
                new LanguageValidationOptions
                {
                    RequiredSet = Iso639Set.Set3
                });
        LanguageValidationResult bibliographic =
            LanguageValidationEngine.ValidateCode(
                "fre",
                new LanguageValidationOptions
                {
                    RequiredForm = LanguageCodeForm.Set2Bibliographic
                });
        LanguageValidationResult set2Bibliographic =
            LanguageValidationEngine.ValidateCode(
                "fre",
                new LanguageValidationOptions
                {
                    RequiredSet = Iso639Set.Set2
                });

        Assert.Multiple(() =>
        {
            Assert.That(set2Terminology.IsCanonical, Is.True);
            Assert.That(set2Terminology.CanonicalValue, Is.Null);
            Assert.That(set3.IsCanonical, Is.True);
            Assert.That(set3.CanonicalValue, Is.Null);
            Assert.That(bibliographic.IsCanonical, Is.True);
            Assert.That(bibliographic.CanonicalValue, Is.Null);
            Assert.That(set2Bibliographic.IsCanonical, Is.False);
            Assert.That(set2Bibliographic.CanonicalValue, Is.EqualTo("fra"));
            Assert.That(set2Bibliographic.Issues[0].Message, Is.EqualTo(
                "The terminology code fra is preferred."));
        });
    }

    [TestCase("iw", "he", LanguageValidationError.DeprecatedCode)]
    [TestCase("adp", "dz", LanguageValidationError.DeprecatedCode)]
    [TestCase("fri", "fry", LanguageValidationError.RetiredCode)]
    public void AuthoritativeLifecycleReplacementsTakePriority(
        string code,
        string expectedCanonical,
        LanguageValidationError lifecycleError)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCode(
                code,
                new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.CanonicalValue, Is.EqualTo(expectedCanonical));
            Assert.That(result.Issues.Select(issue => issue.Error), Is.EqualTo(new[]
            {
                lifecycleError,
                LanguageValidationError.NonCanonical
            }));
            Assert.That(result.Issues.All(issue =>
                issue.Severity == LanguageValidationSeverity.Warning), Is.True);
            Assert.That(result.Issues[1].Message, Is.EqualTo(
                lifecycleError == LanguageValidationError.DeprecatedCode
                    ? $"The authoritative preferred code is {expectedCanonical}."
                    : $"The authoritative replacement code is {expectedCanonical}."));
        });
    }

    [TestCase("agp")]
    [TestCase("aay")]
    public void AmbiguousOrMissingRetirementReplacementIsNeverInvented(string code)
    {
        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCode(
                code,
                new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsCanonical, Is.True);
            Assert.That(result.CanonicalValue, Is.Null);
            Assert.That(result.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.RetiredCode }));
        });
    }

    [Test]
    public void CasingAndCodePreferenceCollapseToOneCanonicalAlternative()
    {
        LanguageValidationResult shortest =
            LanguageValidationEngine.ValidateCode(
                "ENG",
                new LanguageValidationOptions());
        LanguageValidationResult casingOnly =
            LanguageValidationEngine.ValidateCode(
                "AAA",
                new LanguageValidationOptions());
        LanguageValidationResult bibliographic =
            LanguageValidationEngine.ValidateCode(
                "FRE",
                new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(shortest.CanonicalValue, Is.EqualTo("en"));
            Assert.That(shortest.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.NonCanonical }));
            Assert.That(casingOnly.CanonicalValue, Is.EqualTo("aaa"));
            Assert.That(casingOnly.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.NonCanonical }));
            Assert.That(casingOnly.Issues[0].Message, Is.EqualTo(
                "Language codes use lowercase ASCII letters."));
            Assert.That(bibliographic.CanonicalValue, Is.EqualTo("fra"));
            Assert.That(bibliographic.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.NonCanonical }));
        });
    }

    [Test]
    public void StrictCanonicalPolicyRejectsOtherwiseValidAliasesAndCasing()
    {
        LanguageValidationOptions options = new LanguageValidationOptions
        {
            RequireCanonical = true
        };

        LanguageValidationResult bibliographic =
            LanguageValidationEngine.ValidateCode("fre", options);
        LanguageValidationResult uppercase =
            LanguageValidationEngine.ValidateCode("AAA", options);
        LanguageValidationResult preferred =
            LanguageValidationEngine.ValidateCode("en", options);

        Assert.Multiple(() =>
        {
            Assert.That(bibliographic.IsValid, Is.False);
            Assert.That(bibliographic.IsCanonical, Is.False);
            Assert.That(bibliographic.CanonicalValue, Is.EqualTo("fra"));
            Assert.That(bibliographic.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Error));
            Assert.That(uppercase.IsValid, Is.False);
            Assert.That(uppercase.IsCanonical, Is.False);
            Assert.That(uppercase.CanonicalValue, Is.EqualTo("aaa"));
            Assert.That(uppercase.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Error));
            Assert.That(preferred.IsValid, Is.True);
            Assert.That(preferred.IsCanonical, Is.True);
            Assert.That(preferred.Issues, Is.Empty);
        });
    }

    [Test]
    public void ExistingPolicyErrorsDoNotAddDistractingCodePreferences()
    {
        LanguageValidationOptions options = new LanguageValidationOptions
        {
            RequiredForm = LanguageCodeForm.Alpha2
        };

        LanguageValidationResult result =
            LanguageValidationEngine.ValidateCode("eng", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsCanonical, Is.False);
            Assert.That(result.CanonicalValue, Is.Null);
            Assert.That(result.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.WrongCodeForm }));
        });
    }

    [Test]
    public void CanonicalDiagnosticsAreIndependentSnapshots()
    {
        LanguageValidationResult first =
            LanguageValidationEngine.ValidateCode("fre", new LanguageValidationOptions());
        LanguageValidationResult second =
            LanguageValidationEngine.ValidateCode("fre", new LanguageValidationOptions());

        first.Entry!.Set3Code = "changed";
        first.Issues[0].Message = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(second.Entry?.Set3Code, Is.EqualTo("fra"));
            Assert.That(second.CanonicalValue, Is.EqualTo("fra"));
            Assert.That(second.Issues[0].Message, Is.EqualTo(
                "The terminology code fra is preferred."));
            Assert.That(LanguageValidationEngine.ValidateCode(
                "fre",
                new LanguageValidationOptions()).CanonicalValue, Is.EqualTo("fra"));
        });
    }
}
