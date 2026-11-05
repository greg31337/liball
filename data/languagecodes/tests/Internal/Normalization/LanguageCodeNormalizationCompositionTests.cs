using languagecodes;

namespace languagecodes.Tests;

public class LanguageCodeNormalizationCompositionTests
{
    [Test]
    public void AllEnabledStagesProduceOneOrderedAuditedResult()
    {
        LanguageNormalizationOptions options = CreateOptions(
            LanguageCodeForm.Alpha2,
            true,
            true,
            MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
            true,
            true);

        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeCode("  AJP  ", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.OriginalInput, Is.EqualTo("  AJP  "));
            Assert.That(result.Output, Is.EqualTo("ar"));
            Assert.That(result.SourceForm, Is.EqualTo(LanguageCodeForm.IanaLanguage));
            Assert.That(result.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo("ara"));
            Assert.That(result.MacrolanguageRelationship?.ParentCode, Is.EqualTo("ara"));
            Assert.That(result.MacrolanguageRelationship?.ChildCode, Is.EqualTo("apc"));
            Assert.That(result.MacrolanguageRelationship?.IsRetired, Is.False);
            Assert.That(result.AppliedSteps, Is.EqualTo(new[]
            {
                LanguageNormalizationStep.TrimWhitespace,
                LanguageNormalizationStep.NormalizeCase,
                LanguageNormalizationStep.ReplaceDeprecated,
                LanguageNormalizationStep.FoldMacrolanguage,
                LanguageNormalizationStep.ConvertCodeForm
            }));
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.ReplacementCandidates, Is.Empty);
        });
    }

    [Test]
    public void RetirementReplacementPrecedesRequestedFormConversion()
    {
        LanguageNormalizationOptions options = CreateOptions(
            LanguageCodeForm.Alpha2,
            false,
            true,
            MacrolanguageNormalizationPolicy.Preserve,
            true,
            true);

        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeCode("  FRI  ", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.Output, Is.EqualTo("fy"));
            Assert.That(result.SourceForm, Is.EqualTo(LanguageCodeForm.RetiredSet3));
            Assert.That(result.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo("fry"));
            Assert.That(result.AppliedSteps, Is.EqualTo(new[]
            {
                LanguageNormalizationStep.TrimWhitespace,
                LanguageNormalizationStep.NormalizeCase,
                LanguageNormalizationStep.ReplaceRetired,
                LanguageNormalizationStep.ConvertCodeForm
            }));
        });
    }

    [Test]
    public void UnknownTargetPreservesTheCurrentFormAfterEarlierStages()
    {
        LanguageNormalizationResult bibliographic =
            LanguageNormalizationEngine.NormalizeCode(
                "fre",
                CreateOptions(
                    LanguageCodeForm.Unknown,
                    false,
                    false,
                    MacrolanguageNormalizationPolicy.Preserve,
                    false,
                    true));
        LanguageNormalizationResult folded =
            LanguageNormalizationEngine.NormalizeCode(
                "nb",
                CreateOptions(
                    LanguageCodeForm.Unknown,
                    false,
                    false,
                    MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
                    false,
                    true));

        Assert.Multiple(() =>
        {
            Assert.That(bibliographic.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(bibliographic.Output, Is.EqualTo("fre"));
            Assert.That(bibliographic.SourceForm, Is.EqualTo(
                LanguageCodeForm.Set2Bibliographic));
            Assert.That(bibliographic.TargetForm, Is.EqualTo(
                LanguageCodeForm.Set2Bibliographic));
            Assert.That(bibliographic.AppliedSteps, Is.Empty);
            Assert.That(folded.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(folded.Output, Is.EqualTo("nor"));
            Assert.That(folded.SourceForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(folded.TargetForm, Is.EqualTo(LanguageCodeForm.Set3));
            Assert.That(folded.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.FoldMacrolanguage }));
        });
    }

    [Test]
    public void RequestedFormIsAppliedToTheFoldedEntity()
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeCode(
                "nb",
                CreateOptions(
                    LanguageCodeForm.Alpha2,
                    false,
                    false,
                    MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
                    false,
                    true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.Output, Is.EqualTo("no"));
            Assert.That(result.SourceForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(result.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo("nor"));
            Assert.That(result.MacrolanguageRelationship?.ParentCode, Is.EqualTo("nor"));
            Assert.That(result.MacrolanguageRelationship?.ChildCode, Is.EqualTo("nob"));
            Assert.That(result.AppliedSteps, Is.EqualTo(new[]
            {
                LanguageNormalizationStep.FoldMacrolanguage,
                LanguageNormalizationStep.ConvertCodeForm
            }));
        });
    }

    [Test]
    public void HistoricalTerminalOutcomesStopLaterStages()
    {
        LanguageNormalizationOptions options = CreateOptions(
            LanguageCodeForm.Alpha2,
            false,
            true,
            MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
            false,
            true);
        LanguageNormalizationResult split =
            LanguageNormalizationEngine.NormalizeCode("agp", options);
        LanguageNormalizationResult deleted =
            LanguageNormalizationEngine.NormalizeCode("aay", options);

        Assert.Multiple(() =>
        {
            Assert.That(split.Status, Is.EqualTo(LanguageNormalizationStatus.Split));
            Assert.That(split.Output, Is.Null);
            Assert.That(split.SourceForm, Is.EqualTo(LanguageCodeForm.IanaLanguage));
            Assert.That(split.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(split.Entry?.Retirement?.RetiredCode, Is.EqualTo("agp"));
            Assert.That(split.ReplacementCandidates, Is.EqualTo(new[] { "apf", "prf" }));
            Assert.That(split.MacrolanguageRelationship, Is.Null);
            Assert.That(split.AppliedSteps, Is.Empty);
            Assert.That(deleted.Status, Is.EqualTo(
                LanguageNormalizationStatus.NoReplacement));
            Assert.That(deleted.Output, Is.Null);
            Assert.That(deleted.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(deleted.ReplacementCandidates, Is.Empty);
            Assert.That(deleted.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void TargetFailureRetainsTheAlreadyAppliedRelationship()
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeCode(
                "bcl",
                CreateOptions(
                    LanguageCodeForm.Alpha2,
                    false,
                    false,
                    MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
                    false,
                    true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(
                LanguageNormalizationStatus.TargetUnavailable));
            Assert.That(result.Output, Is.Null);
            Assert.That(result.SourceForm, Is.EqualTo(LanguageCodeForm.Set3));
            Assert.That(result.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo("bik"));
            Assert.That(result.MacrolanguageRelationship?.ParentCode, Is.EqualTo("bik"));
            Assert.That(result.MacrolanguageRelationship?.ChildCode, Is.EqualTo("bcl"));
            Assert.That(result.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.FoldMacrolanguage }));
        });
    }

    [Test]
    public void TextuallyIdenticalCrossFormTargetIsAlreadyCanonical()
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeCode(
                "fra",
                CreateOptions(
                    LanguageCodeForm.Set3,
                    false,
                    false,
                    MacrolanguageNormalizationPolicy.Preserve,
                    false,
                    true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(result.Output, Is.EqualTo("fra"));
            Assert.That(result.SourceForm, Is.EqualTo(
                LanguageCodeForm.Set2Terminology));
            Assert.That(result.TargetForm, Is.EqualTo(LanguageCodeForm.Set3));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo("fra"));
            Assert.That(result.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void LexicalFailuresRetainTheRequestedTargetWithoutRunningOtherStages()
    {
        LanguageNormalizationOptions options = CreateOptions(
            LanguageCodeForm.Set3,
            true,
            true,
            MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
            false,
            true);
        LanguageNormalizationResult malformed =
            LanguageNormalizationEngine.NormalizeCode("e1g", options);
        LanguageNormalizationResult missing =
            LanguageNormalizationEngine.NormalizeCode("zzz", options);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Status, Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(malformed.Output, Is.Null);
            Assert.That(malformed.SourceForm, Is.EqualTo(LanguageCodeForm.Unknown));
            Assert.That(malformed.TargetForm, Is.EqualTo(LanguageCodeForm.Set3));
            Assert.That(missing.Status, Is.EqualTo(LanguageNormalizationStatus.NotFound));
            Assert.That(missing.Output, Is.Null);
            Assert.That(missing.TargetForm, Is.EqualTo(LanguageCodeForm.Set3));
            Assert.That(malformed.AppliedSteps, Is.Empty);
            Assert.That(missing.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void ComplexNormalizationOutputIsStableOnASecondPass()
    {
        LanguageNormalizationOptions options = CreateOptions(
            LanguageCodeForm.Alpha2,
            true,
            true,
            MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
            true,
            true);
        LanguageNormalizationResult first =
            LanguageNormalizationEngine.NormalizeCode("  AJP  ", options);
        LanguageNormalizationResult second =
            LanguageNormalizationEngine.NormalizeCode(first.Output, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Output, Is.EqualTo("ar"));
            Assert.That(second.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(second.Output, Is.EqualTo(first.Output));
            Assert.That(second.SourceForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(second.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(second.Entry?.Set3Code, Is.EqualTo("ara"));
            Assert.That(second.MacrolanguageRelationship, Is.Null);
            Assert.That(second.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void ComposedResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageNormalizationOptions options = CreateOptions(
            LanguageCodeForm.Alpha2,
            true,
            true,
            MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
            false,
            true);
        LanguageNormalizationResult first =
            LanguageNormalizationEngine.NormalizeCode("ajp", options);
        LanguageNormalizationResult second =
            LanguageNormalizationEngine.NormalizeCode("ajp", options);
        string originalName = second.Entry!.Names[0].Text!;

        first.Entry!.Set3Code = "changed";
        first.Entry.Names[0].Text = "changed";
        first.MacrolanguageRelationship!.ParentCode = "changed";
        first.AppliedSteps[0] = LanguageNormalizationStep.NormalizeCase;

        Assert.Multiple(() =>
        {
            Assert.That(second.Output, Is.EqualTo("ar"));
            Assert.That(second.Entry?.Set3Code, Is.EqualTo("ara"));
            Assert.That(second.Entry?.Names[0].Text, Is.EqualTo(originalName));
            Assert.That(second.MacrolanguageRelationship?.ParentCode, Is.EqualTo("ara"));
            Assert.That(second.AppliedSteps, Is.EqualTo(new[]
            {
                LanguageNormalizationStep.ReplaceDeprecated,
                LanguageNormalizationStep.FoldMacrolanguage,
                LanguageNormalizationStep.ConvertCodeForm
            }));
            Assert.That(LanguageNormalizationEngine.NormalizeCode("ajp", options).Output,
                Is.EqualTo("ar"));
        });
    }

    private static LanguageNormalizationOptions CreateOptions(
        LanguageCodeForm targetForm,
        bool replaceDeprecated,
        bool replaceRetired,
        MacrolanguageNormalizationPolicy macrolanguagePolicy,
        bool trimWhitespace,
        bool normalizeCase)
    {
        return new LanguageNormalizationOptions
        {
            TargetForm = targetForm,
            ReplaceDeprecated = replaceDeprecated,
            ReplaceRetired = replaceRetired,
            MacrolanguagePolicy = macrolanguagePolicy,
            TrimWhitespace = trimWhitespace,
            NormalizeCase = normalizeCase
        };
    }
}
