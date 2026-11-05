using languagecodes;

namespace languagecodes.Tests;

public class LanguageCanonicalizationEngineTests
{
    [Test]
    public void PreserveFormChangesOnlyEnabledLexicalDetails()
    {
        LanguageNormalizationResult changed =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                "FRE",
                CreateOptions(LanguageCodeForm.Unknown, false, true));
        LanguageNormalizationResult canonical =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                "fre",
                CreateOptions(LanguageCodeForm.Unknown, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(changed.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(changed.Output, Is.EqualTo("fre"));
            Assert.That(changed.SourceForm, Is.EqualTo(LanguageCodeForm.Set2Bibliographic));
            Assert.That(changed.TargetForm, Is.EqualTo(LanguageCodeForm.Set2Bibliographic));
            Assert.That(changed.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.NormalizeCase }));
            Assert.That(canonical.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(canonical.Output, Is.EqualTo("fre"));
            Assert.That(canonical.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void ShortestPolicySelectsSet1WhenAvailable()
    {
        LanguageNormalizationResult converted =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                "eng",
                CreateOptions(LanguageCodeForm.Alpha2, false, true));
        LanguageNormalizationResult alreadyShortest =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                "en",
                CreateOptions(LanguageCodeForm.Alpha2, false, true));
        LanguageNormalizationResult alias =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                "iw",
                CreateOptions(LanguageCodeForm.Alpha2, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(converted.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(converted.Output, Is.EqualTo("en"));
            Assert.That(converted.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(converted.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(converted.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ConvertCodeForm }));
            Assert.That(alreadyShortest.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(alreadyShortest.Output, Is.EqualTo("en"));
            Assert.That(alreadyShortest.AppliedSteps, Is.Empty);
            Assert.That(alias.Output, Is.EqualTo("he"));
            Assert.That(alias.Entry?.Set3Code, Is.EqualTo("heb"));
            Assert.That(alias.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ConvertCodeForm }));
        });
    }

    [Test]
    public void Set3AndTerminologyPoliciesShareDeterministicAlpha3Fallbacks()
    {
        LanguageNormalizationResult fromBibliographic =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                "fre",
                CreateOptions(LanguageCodeForm.Set3, false, true));
        LanguageNormalizationResult set5Collection =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                "afa",
                CreateOptions(LanguageCodeForm.Set3, false, true));
        LanguageNormalizationResult set3Only =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                "aaa",
                CreateOptions(LanguageCodeForm.Set2Terminology, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(fromBibliographic.Output, Is.EqualTo("fra"));
            Assert.That(fromBibliographic.TargetForm, Is.EqualTo(LanguageCodeForm.Set3));
            Assert.That(fromBibliographic.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ConvertCodeForm }));
            Assert.That(set5Collection.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(set5Collection.Output, Is.EqualTo("afa"));
            Assert.That(
                set5Collection.TargetForm,
                Is.EqualTo(LanguageCodeForm.Set2Terminology));
            Assert.That(set3Only.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(set3Only.Output, Is.EqualTo("aaa"));
            Assert.That(set3Only.TargetForm, Is.EqualTo(LanguageCodeForm.Set3));
        });
    }

    [Test]
    public void BibliographicPolicySelectsTheRegisteredAlternateCode()
    {
        LanguageNormalizationResult converted =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                "fra",
                CreateOptions(LanguageCodeForm.Set2Bibliographic, false, true));
        LanguageNormalizationResult canonical =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                "fre",
                CreateOptions(LanguageCodeForm.Set2Bibliographic, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(converted.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(converted.Output, Is.EqualTo("fre"));
            Assert.That(
                converted.TargetForm,
                Is.EqualTo(LanguageCodeForm.Set2Bibliographic));
            Assert.That(converted.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ConvertCodeForm }));
            Assert.That(canonical.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(canonical.Output, Is.EqualTo("fre"));
        });
    }

    [TestCase("aaa", LanguageCodeForm.Alpha2)]
    [TestCase("aaa", LanguageCodeForm.Set2Bibliographic)]
    [TestCase("gmw", LanguageCodeForm.Set3)]
    [TestCase("eng", LanguageCodeForm.Set5)]
    public void MissingOrUnsupportedTargetsAreExplicitlyUnavailable(
        string code,
        LanguageCodeForm targetForm)
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                code,
                CreateOptions(targetForm, false, true));

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

    [TestCase("fri", LanguageCodeForm.Set3)]
    [TestCase("qaa", LanguageCodeForm.Unknown)]
    public void NonActiveEntitiesAreIncompatibleWithActiveCanonicalization(
        string code,
        LanguageCodeForm targetForm)
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                code,
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

    [Test]
    public void LexicalFailuresPropagateWithTheRequestedTarget()
    {
        LanguageNormalizationResult invalid =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                "e1g",
                CreateOptions(LanguageCodeForm.Alpha2, false, true));
        LanguageNormalizationResult missing =
            LanguageNormalizationEngine.CanonicalizeActiveCode(
                "zzz",
                CreateOptions(LanguageCodeForm.Set3, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status, Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(invalid.Output, Is.Null);
            Assert.That(invalid.SourceForm, Is.EqualTo(LanguageCodeForm.Unknown));
            Assert.That(invalid.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(missing.Status, Is.EqualTo(LanguageNormalizationStatus.NotFound));
            Assert.That(missing.Output, Is.Null);
            Assert.That(missing.TargetForm, Is.EqualTo(LanguageCodeForm.Set3));
        });
    }

    [Test]
    public void CombinedLexicalAndFormChangesRemainOrderedAndIdempotent()
    {
        LanguageNormalizationOptions options = CreateOptions(
            LanguageCodeForm.Set2Bibliographic,
            true,
            true);
        LanguageNormalizationResult first =
            LanguageNormalizationEngine.CanonicalizeActiveCode("  FRA  ", options);
        LanguageNormalizationResult second =
            LanguageNormalizationEngine.CanonicalizeActiveCode(first.Output, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(first.Output, Is.EqualTo("fre"));
            Assert.That(
                first.AppliedSteps,
                Is.EqualTo(new[]
                {
                    LanguageNormalizationStep.TrimWhitespace,
                    LanguageNormalizationStep.NormalizeCase,
                    LanguageNormalizationStep.ConvertCodeForm
                }));
            Assert.That(second.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(second.Output, Is.EqualTo(first.Output));
            Assert.That(second.TargetForm, Is.EqualTo(
                LanguageCodeForm.Set2Bibliographic));
            Assert.That(second.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void CanonicalizationResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageNormalizationOptions options = CreateOptions(
            LanguageCodeForm.Alpha2,
            false,
            true);
        LanguageNormalizationResult first =
            LanguageNormalizationEngine.CanonicalizeActiveCode("eng", options);
        LanguageNormalizationResult second =
            LanguageNormalizationEngine.CanonicalizeActiveCode("eng", options);
        string originalName = second.Entry!.Names[0].Text!;

        first.AppliedSteps[0] = LanguageNormalizationStep.NormalizeCase;
        first.Entry!.Alpha2Code = "changed";
        first.Entry.Names[0].Text = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Entry, Is.Not.SameAs(second.Entry));
            Assert.That(second.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ConvertCodeForm }));
            Assert.That(second.Entry?.Alpha2Code, Is.EqualTo("en"));
            Assert.That(second.Entry?.Names[0].Text, Is.EqualTo(originalName));
            Assert.That(
                LanguageNormalizationEngine.CanonicalizeActiveCode("eng", options).Output,
                Is.EqualTo("en"));
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
