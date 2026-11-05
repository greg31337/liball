using languagecodes;

namespace languagecodes.Tests;

public class LanguageNormalizationDataTests
{
    [Test]
    public void NormalizationOptionsStoreCodeAndTagPolicies()
    {
        LanguageNormalizationOptions options = new LanguageNormalizationOptions
        {
            TargetForm = LanguageCodeForm.Alpha2,
            ReplaceDeprecated = true,
            ReplaceRetired = true,
            MacrolanguagePolicy = MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
            TrimWhitespace = true,
            NormalizeCase = true,
            NormalizeTagSeparators = true,
            NormalizeTagCasing = true,
            ReplaceGrandfatheredTags = true,
            RemoveSuppressScript = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(options.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(options.ReplaceDeprecated, Is.True);
            Assert.That(options.ReplaceRetired, Is.True);
            Assert.That(
                options.MacrolanguagePolicy,
                Is.EqualTo(MacrolanguageNormalizationPolicy.FoldToMacrolanguage));
            Assert.That(options.TrimWhitespace, Is.True);
            Assert.That(options.NormalizeCase, Is.True);
            Assert.That(options.NormalizeTagSeparators, Is.True);
            Assert.That(options.NormalizeTagCasing, Is.True);
            Assert.That(options.ReplaceGrandfatheredTags, Is.True);
            Assert.That(options.RemoveSuppressScript, Is.True);
        });
    }

    [Test]
    public void NormalizationResultRepresentsSuccessfulCodeConversion()
    {
        LanguageCodeEntry french = new LanguageCodeEntry
        {
            Alpha2Code = "fr",
            Set3Code = "fra",
            ReferenceName = "French",
            Status = LanguageCodeStatus.Active
        };
        LanguageNormalizationResult result = new LanguageNormalizationResult
        {
            Status = LanguageNormalizationStatus.Success,
            OriginalInput = " FRA ",
            Output = "fr",
            SourceForm = LanguageCodeForm.Set3,
            TargetForm = LanguageCodeForm.Alpha2,
            Entry = french,
            AppliedSteps = new[]
            {
                LanguageNormalizationStep.TrimWhitespace,
                LanguageNormalizationStep.NormalizeCase,
                LanguageNormalizationStep.ConvertCodeForm
            },
            Warnings = new[] { "Input used a longer code form." }
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.OriginalInput, Is.EqualTo(" FRA "));
            Assert.That(result.Output, Is.EqualTo("fr"));
            Assert.That(result.SourceForm, Is.EqualTo(LanguageCodeForm.Set3));
            Assert.That(result.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(result.Entry, Is.SameAs(french));
            Assert.That(
                result.AppliedSteps,
                Is.EqualTo(new[]
                {
                    LanguageNormalizationStep.TrimWhitespace,
                    LanguageNormalizationStep.NormalizeCase,
                    LanguageNormalizationStep.ConvertCodeForm
                }));
            Assert.That(result.Warnings, Is.EqualTo(new[] { "Input used a longer code form." }));
            Assert.That(result.ReplacementCandidates, Is.Empty);
        });
    }

    [Test]
    public void NormalizationResultRepresentsSplitRetirement()
    {
        LanguageCodeEntry retired = new LanguageCodeEntry
        {
            Set3Code = "agp",
            Status = LanguageCodeStatus.Retired,
            Retirement = new LanguageRetirement
            {
                RetiredCode = "agp",
                Reason = LanguageRetirementReason.Split,
                ReplacementCodes = new[] { "apf", "prf" }
            }
        };
        LanguageNormalizationResult result = new LanguageNormalizationResult
        {
            Status = LanguageNormalizationStatus.Split,
            OriginalInput = "agp",
            SourceForm = LanguageCodeForm.RetiredSet3,
            TargetForm = LanguageCodeForm.Set3,
            Entry = retired,
            ReplacementCandidates = new[] { "apf", "prf" }
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Split));
            Assert.That(result.Output, Is.Null);
            Assert.That(result.Entry, Is.SameAs(retired));
            Assert.That(result.AppliedSteps, Is.Empty);
            Assert.That(result.ReplacementCandidates, Is.EqualTo(new[] { "apf", "prf" }));
        });
    }

    [Test]
    public void NormalizationResultRecordsMacrolanguageFolding()
    {
        LanguageRelationship relationship = new LanguageRelationship
        {
            ParentCode = "ara",
            ChildCode = "aao",
            Kind = LanguageRelationshipKind.MacrolanguageMember,
            SourceSet = Iso639Set.Set3
        };
        LanguageNormalizationResult result = new LanguageNormalizationResult
        {
            Status = LanguageNormalizationStatus.Success,
            OriginalInput = "aao",
            Output = "ara",
            MacrolanguageRelationship = relationship,
            AppliedSteps = new[] { LanguageNormalizationStep.FoldMacrolanguage }
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.MacrolanguageRelationship, Is.SameAs(relationship));
            Assert.That(result.Output, Is.EqualTo("ara"));
            Assert.That(result.AppliedSteps, Is.EqualTo(new[] { LanguageNormalizationStep.FoldMacrolanguage }));
        });
    }

    [Test]
    public void NormalizationResultInstancesDoNotShareArrays()
    {
        LanguageNormalizationResult first = new LanguageNormalizationResult();
        LanguageNormalizationResult second = new LanguageNormalizationResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.AppliedSteps, Is.Empty);
            Assert.That(first.Warnings, Is.Empty);
            Assert.That(first.ReplacementCandidates, Is.Empty);
            Assert.That(first.AppliedSteps, Is.Not.SameAs(second.AppliedSteps));
            Assert.That(first.Warnings, Is.Not.SameAs(second.Warnings));
            Assert.That(first.ReplacementCandidates, Is.Not.SameAs(second.ReplacementCandidates));
        });
    }

    [Test]
    public void NormalizationOperationRowKeepsOptionsAndResultTogether()
    {
        LanguageNormalizationOptions options = new LanguageNormalizationOptions
        {
            TargetForm = LanguageCodeForm.Set3
        };
        LanguageNormalizationResult result = new LanguageNormalizationResult
        {
            Status = LanguageNormalizationStatus.AlreadyCanonical,
            Output = "eng"
        };
        NormalizationOperationRow operation = new NormalizationOperationRow(options, result);

        Assert.Multiple(() =>
        {
            Assert.That(operation.Options, Is.SameAs(options));
            Assert.That(operation.Result, Is.SameAs(result));
        });
    }
}
