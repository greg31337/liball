using languagecodes;

namespace languagecodes.Tests;

public class LanguageNormalizationEngineTests
{
    [Test]
    public void TrimmingAndCasingProduceAnOrderedLexicalAuditTrail()
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeCodeLexically(
                "  EnG  ",
                CreateOptions(true, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.OriginalInput, Is.EqualTo("  EnG  "));
            Assert.That(result.Output, Is.EqualTo("eng"));
            Assert.That(result.SourceForm, Is.EqualTo(LanguageCodeForm.Set2Terminology));
            Assert.That(result.TargetForm, Is.EqualTo(LanguageCodeForm.Set2Terminology));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(
                result.AppliedSteps,
                Is.EqualTo(new[]
                {
                    LanguageNormalizationStep.TrimWhitespace,
                    LanguageNormalizationStep.NormalizeCase
                }));
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.ReplacementCandidates, Is.Empty);
        });
    }

    [Test]
    public void TrimmingAndCaseNormalizationAreIndependentPolicies()
    {
        LanguageNormalizationResult noTrim =
            LanguageNormalizationEngine.NormalizeCodeLexically(
                "  ENG  ",
                CreateOptions(false, true));
        LanguageNormalizationResult noCase =
            LanguageNormalizationEngine.NormalizeCodeLexically(
                "ENG",
                CreateOptions(false, false));
        LanguageNormalizationResult trimOnly =
            LanguageNormalizationEngine.NormalizeCodeLexically(
                "  eng  ",
                CreateOptions(true, false));
        LanguageNormalizationResult caseOnly =
            LanguageNormalizationEngine.NormalizeCodeLexically(
                "ENG",
                CreateOptions(false, true));

        Assert.Multiple(() =>
        {
            Assert.That(noTrim.Status, Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(noTrim.Output, Is.Null);
            Assert.That(noTrim.AppliedSteps, Is.Empty);
            Assert.That(noCase.Status, Is.EqualTo(LanguageNormalizationStatus.NotFound));
            Assert.That(noCase.Output, Is.Null);
            Assert.That(noCase.AppliedSteps, Is.Empty);
            Assert.That(trimOnly.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(trimOnly.Output, Is.EqualTo("eng"));
            Assert.That(trimOnly.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.TrimWhitespace }));
            Assert.That(caseOnly.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(caseOnly.Output, Is.EqualTo("eng"));
            Assert.That(caseOnly.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.NormalizeCase }));
        });
    }

    [TestCase("", TestName = "Empty")]
    [TestCase("e", TestName = "TooShort")]
    [TestCase("engl", TestName = "TooLong")]
    [TestCase("e1g", TestName = "Digit")]
    [TestCase("en-", TestName = "Separator")]
    [TestCase("\u00e9n", TestName = "NonAscii")]
    public void MalformedCodesAreInvalid(string code)
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeCodeLexically(
                code,
                CreateOptions(true, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(result.OriginalInput, Is.EqualTo(code));
            Assert.That(result.Output, Is.Null);
            Assert.That(result.SourceForm, Is.EqualTo(LanguageCodeForm.Unknown));
            Assert.That(result.TargetForm, Is.EqualTo(LanguageCodeForm.Unknown));
        });
    }

    [Test]
    public void NullCodeIsInvalidWithoutInventingOutput()
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeCodeLexically(
                null,
                CreateOptions(true, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(result.OriginalInput, Is.Null);
            Assert.That(result.Output, Is.Null);
            Assert.That(result.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void WellFormedUnassignedCodesRemainDistinctFromInvalidCodes()
    {
        LanguageNormalizationResult lower =
            LanguageNormalizationEngine.NormalizeCodeLexically(
                "zz",
                CreateOptions(false, true));
        LanguageNormalizationResult upper =
            LanguageNormalizationEngine.NormalizeCodeLexically(
                "ZZ",
                CreateOptions(false, true));

        Assert.Multiple(() =>
        {
            Assert.That(lower.Status, Is.EqualTo(LanguageNormalizationStatus.NotFound));
            Assert.That(lower.Output, Is.Null);
            Assert.That(lower.SourceForm, Is.EqualTo(LanguageCodeForm.Unknown));
            Assert.That(lower.AppliedSteps, Is.Empty);
            Assert.That(upper.Status, Is.EqualTo(LanguageNormalizationStatus.NotFound));
            Assert.That(upper.Output, Is.Null);
            Assert.That(upper.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.NormalizeCase }));
        });
    }

    [TestCase("en", LanguageCodeForm.Alpha2, "eng")]
    [TestCase("fre", LanguageCodeForm.Set2Bibliographic, "fra")]
    [TestCase("aaa", LanguageCodeForm.Set3, "aaa")]
    [TestCase("gmw", LanguageCodeForm.Set5, null)]
    [TestCase("fri", LanguageCodeForm.RetiredSet3, "fri")]
    [TestCase("qaa", LanguageCodeForm.PrivateUse, null)]
    [TestCase("iw", LanguageCodeForm.IanaLanguage, "heb")]
    public void AssignedFormsAreClassifiedDeterministically(
        string code,
        LanguageCodeForm expectedForm,
        string? expectedSet3Code)
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeCodeLexically(
                code,
                CreateOptions(false, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(result.Output, Is.EqualTo(code));
            Assert.That(result.SourceForm, Is.EqualTo(expectedForm));
            Assert.That(result.TargetForm, Is.EqualTo(expectedForm));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo(expectedSet3Code));
            Assert.That(result.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void ActiveIsoFormsOutrankEquivalentAliasesAndOtherSetForms()
    {
        LanguageNormalizationResult aao =
            LanguageNormalizationEngine.NormalizeCodeLexically(
                "aao",
                CreateOptions(false, true));
        LanguageNormalizationResult eng =
            LanguageNormalizationEngine.NormalizeCodeLexically(
                "eng",
                CreateOptions(false, true));

        Assert.Multiple(() =>
        {
            Assert.That(aao.SourceForm, Is.EqualTo(LanguageCodeForm.Set3));
            Assert.That(aao.Entry?.Set3Code, Is.EqualTo("aao"));
            Assert.That(eng.SourceForm, Is.EqualTo(LanguageCodeForm.Set2Terminology));
            Assert.That(eng.Entry?.Set3Code, Is.EqualTo("eng"));
        });
    }

    [Test]
    public void ASecondLexicalPassIsAlreadyCanonical()
    {
        LanguageNormalizationOptions options = CreateOptions(true, true);
        LanguageNormalizationResult first =
            LanguageNormalizationEngine.NormalizeCodeLexically("  ENG  ", options);
        LanguageNormalizationResult second =
            LanguageNormalizationEngine.NormalizeCodeLexically(first.Output, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(second.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(second.OriginalInput, Is.EqualTo(first.Output));
            Assert.That(second.Output, Is.EqualTo(first.Output));
            Assert.That(second.SourceForm, Is.EqualTo(first.SourceForm));
            Assert.That(second.TargetForm, Is.EqualTo(first.TargetForm));
            Assert.That(second.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void LexicalResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageNormalizationOptions options = CreateOptions(false, true);
        LanguageNormalizationResult first =
            LanguageNormalizationEngine.NormalizeCodeLexically("ENG", options);
        LanguageNormalizationResult second =
            LanguageNormalizationEngine.NormalizeCodeLexically("ENG", options);
        string originalName = second.Entry!.Names[0].Text!;

        first.AppliedSteps[0] = LanguageNormalizationStep.ConvertCodeForm;
        first.Entry!.Set3Code = "changed";
        first.Entry.Names[0].Text = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.AppliedSteps, Is.Not.SameAs(second.AppliedSteps));
            Assert.That(second.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.NormalizeCase }));
            Assert.That(second.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(second.Entry?.Names[0].Text, Is.EqualTo(originalName));
            Assert.That(
                LanguageNormalizationEngine.NormalizeCodeLexically("ENG", options)
                    .Entry?.Set3Code,
                Is.EqualTo("eng"));
        });
    }

    private static LanguageNormalizationOptions CreateOptions(
        bool trimWhitespace,
        bool normalizeCase)
    {
        return new LanguageNormalizationOptions
        {
            TrimWhitespace = trimWhitespace,
            NormalizeCase = normalizeCase
        };
    }
}
