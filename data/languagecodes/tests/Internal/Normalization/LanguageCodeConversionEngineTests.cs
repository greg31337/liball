using languagecodes;

namespace languagecodes.Tests;

public class LanguageCodeConversionEngineTests
{
    [TestCase("fr", LanguageCodeForm.Alpha2, "fr")]
    [TestCase("fr", LanguageCodeForm.Set2Terminology, "fra")]
    [TestCase("fr", LanguageCodeForm.Set2Bibliographic, "fre")]
    [TestCase("fr", LanguageCodeForm.Set3, "fra")]
    [TestCase("fra", LanguageCodeForm.Alpha2, "fr")]
    [TestCase("fra", LanguageCodeForm.Set2Terminology, "fra")]
    [TestCase("fra", LanguageCodeForm.Set2Bibliographic, "fre")]
    [TestCase("fra", LanguageCodeForm.Set3, "fra")]
    [TestCase("fre", LanguageCodeForm.Alpha2, "fr")]
    [TestCase("fre", LanguageCodeForm.Set2Terminology, "fra")]
    [TestCase("fre", LanguageCodeForm.Set2Bibliographic, "fre")]
    [TestCase("fre", LanguageCodeForm.Set3, "fra")]
    public void ConvertsAcrossEveryAvailableCodeForm(
        string input,
        LanguageCodeForm targetForm,
        string expected)
    {
        LanguageNormalizationResult result = LanguageNormalizationEngine.ConvertCode(
            input,
            CreateOptions(targetForm, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Output, Is.EqualTo(expected));
            Assert.That(result.SourceForm, Is.EqualTo(
                input == "fr"
                    ? LanguageCodeForm.Alpha2
                    : input == "fre"
                        ? LanguageCodeForm.Set2Bibliographic
                        : LanguageCodeForm.Set2Terminology));
            Assert.That(result.TargetForm, Is.EqualTo(targetForm));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo("fra"));
            Assert.That(
                result.Status,
                Is.EqualTo(
                    input == expected
                        ? LanguageNormalizationStatus.AlreadyCanonical
                        : LanguageNormalizationStatus.Success));
            Assert.That(
                result.AppliedSteps,
                Is.EqualTo(
                    input == expected
                        ? Array.Empty<LanguageNormalizationStep>()
                        : new[] { LanguageNormalizationStep.ConvertCodeForm }));
        });
    }

    [Test]
    public void PreserveTargetKeepsTheRecognizedInputForm()
    {
        LanguageNormalizationResult result = LanguageNormalizationEngine.ConvertCode(
            "fre",
            CreateOptions(LanguageCodeForm.Unknown, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(result.Output, Is.EqualTo("fre"));
            Assert.That(result.SourceForm, Is.EqualTo(LanguageCodeForm.Set2Bibliographic));
            Assert.That(result.TargetForm, Is.EqualTo(LanguageCodeForm.Set2Bibliographic));
            Assert.That(result.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void ExactConversionDoesNotRelabelMissingSetFields()
    {
        LanguageNormalizationResult terminology = LanguageNormalizationEngine.ConvertCode(
            "aaa",
            CreateOptions(LanguageCodeForm.Set2Terminology, false, true));
        LanguageNormalizationResult bibliographic = LanguageNormalizationEngine.ConvertCode(
            "aaa",
            CreateOptions(LanguageCodeForm.Set2Bibliographic, false, true));
        LanguageNormalizationResult alpha2 = LanguageNormalizationEngine.ConvertCode(
            "aaa",
            CreateOptions(LanguageCodeForm.Alpha2, false, true));
        LanguageNormalizationResult set3Collection = LanguageNormalizationEngine.ConvertCode(
            "afa",
            CreateOptions(LanguageCodeForm.Set3, false, true));
        LanguageNormalizationResult canonicalPolicy =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                "aaa",
                CreateOptions(LanguageCodeForm.Set2Terminology, false, true));

        Assert.Multiple(() =>
        {
            AssertUnavailable(terminology, LanguageCodeForm.Set2Terminology);
            AssertUnavailable(bibliographic, LanguageCodeForm.Set2Bibliographic);
            AssertUnavailable(alpha2, LanguageCodeForm.Alpha2);
            AssertUnavailable(set3Collection, LanguageCodeForm.Set3);
            Assert.That(canonicalPolicy.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(canonicalPolicy.Output, Is.EqualTo("aaa"));
            Assert.That(canonicalPolicy.TargetForm, Is.EqualTo(LanguageCodeForm.Set3));
        });
    }

    [TestCase("gmw", LanguageCodeForm.Unknown)]
    [TestCase("qaa", LanguageCodeForm.Unknown)]
    [TestCase("fri", LanguageCodeForm.Set3)]
    [TestCase("iw", LanguageCodeForm.Alpha2)]
    public void UnsupportedSourceFormsOrEntitiesAreIncompatible(
        string input,
        LanguageCodeForm targetForm)
    {
        LanguageNormalizationResult result = LanguageNormalizationEngine.ConvertCode(
            input,
            CreateOptions(targetForm, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(
                LanguageNormalizationStatus.IncompatibleEntity));
            Assert.That(result.Output, Is.Null);
            Assert.That(result.TargetForm, Is.EqualTo(
                targetForm == LanguageCodeForm.Unknown
                    ? result.SourceForm
                    : targetForm));
            Assert.That(result.AppliedSteps, Is.Empty);
        });
    }

    [TestCase(LanguageCodeForm.Set5)]
    [TestCase(LanguageCodeForm.RetiredSet3)]
    [TestCase(LanguageCodeForm.IanaLanguage)]
    [TestCase(LanguageCodeForm.PrivateUse)]
    public void UnsupportedTargetFormsAreUnavailable(LanguageCodeForm targetForm)
    {
        LanguageNormalizationResult result = LanguageNormalizationEngine.ConvertCode(
            "eng",
            CreateOptions(targetForm, false, true));

        AssertUnavailable(result, targetForm);
    }

    [Test]
    public void LexicalFailuresPropagateWithoutFabricatedCodes()
    {
        LanguageNormalizationResult invalid = LanguageNormalizationEngine.ConvertCode(
            "e1g",
            CreateOptions(LanguageCodeForm.Alpha2, false, true));
        LanguageNormalizationResult missing = LanguageNormalizationEngine.ConvertCode(
            "zzz",
            CreateOptions(LanguageCodeForm.Set3, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status, Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(invalid.Output, Is.Null);
            Assert.That(invalid.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(missing.Status, Is.EqualTo(LanguageNormalizationStatus.NotFound));
            Assert.That(missing.Output, Is.Null);
            Assert.That(missing.TargetForm, Is.EqualTo(LanguageCodeForm.Set3));
        });
    }

    [Test]
    public void LexicalAndConversionStepsRetainDeterministicOrder()
    {
        LanguageNormalizationResult result = LanguageNormalizationEngine.ConvertCode(
            "  FRE  ",
            CreateOptions(LanguageCodeForm.Alpha2, true, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.OriginalInput, Is.EqualTo("  FRE  "));
            Assert.That(result.Output, Is.EqualTo("fr"));
            Assert.That(
                result.AppliedSteps,
                Is.EqualTo(new[]
                {
                    LanguageNormalizationStep.TrimWhitespace,
                    LanguageNormalizationStep.NormalizeCase,
                    LanguageNormalizationStep.ConvertCodeForm
                }));
        });
    }

    [Test]
    public void ConvertedOutputIsStableOnASecondPass()
    {
        LanguageNormalizationOptions options = CreateOptions(
            LanguageCodeForm.Alpha2,
            true,
            true);
        LanguageNormalizationResult first =
            LanguageNormalizationEngine.ConvertCode("  FRE  ", options);
        LanguageNormalizationResult second =
            LanguageNormalizationEngine.ConvertCode(first.Output, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Output, Is.EqualTo("fr"));
            Assert.That(second.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(second.Output, Is.EqualTo(first.Output));
            Assert.That(second.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(second.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void ConversionResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageNormalizationOptions options = CreateOptions(
            LanguageCodeForm.Alpha2,
            false,
            true);
        LanguageNormalizationResult first =
            LanguageNormalizationEngine.ConvertCode("fra", options);
        LanguageNormalizationResult second =
            LanguageNormalizationEngine.ConvertCode("fra", options);
        string originalName = second.Entry!.Names[0].Text!;

        first.AppliedSteps[0] = LanguageNormalizationStep.NormalizeCase;
        first.Entry!.Alpha2Code = "changed";
        first.Entry.Names[0].Text = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Entry, Is.Not.SameAs(second.Entry));
            Assert.That(second.Output, Is.EqualTo("fr"));
            Assert.That(second.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ConvertCodeForm }));
            Assert.That(second.Entry?.Alpha2Code, Is.EqualTo("fr"));
            Assert.That(second.Entry?.Names[0].Text, Is.EqualTo(originalName));
            Assert.That(LanguageNormalizationEngine.ConvertCode("fra", options).Output, Is.EqualTo("fr"));
        });
    }

    private static void AssertUnavailable(
        LanguageNormalizationResult result,
        LanguageCodeForm targetForm)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(
                LanguageNormalizationStatus.TargetUnavailable));
            Assert.That(result.Output, Is.Null);
            Assert.That(result.TargetForm, Is.EqualTo(targetForm));
            Assert.That(result.Entry, Is.Not.Null);
            Assert.That(result.AppliedSteps, Is.Empty);
        });
    }

    private static LanguageNormalizationOptions CreateOptions(
        LanguageCodeForm targetForm,
        bool trimWhitespace,
        bool normalizeCase)
    {
        return new LanguageNormalizationOptions
        {
            TargetForm = targetForm,
            TrimWhitespace = trimWhitespace,
            NormalizeCase = normalizeCase
        };
    }
}
