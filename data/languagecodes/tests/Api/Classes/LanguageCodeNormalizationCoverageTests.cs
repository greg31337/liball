using languagecodes;

namespace languagecodes.Tests;

public sealed class LanguageCodeNormalizationCoverageTests
{
    [TestCase(
        "en",
        LanguageCodeForm.Alpha2,
        LanguageCodeForm.Alpha2,
        "en",
        LanguageNormalizationStatus.AlreadyCanonical)]
    [TestCase(
        "fra",
        LanguageCodeForm.Set2Terminology,
        LanguageCodeForm.Set2Bibliographic,
        "fre",
        LanguageNormalizationStatus.Success)]
    [TestCase(
        "fre",
        LanguageCodeForm.Set2Bibliographic,
        LanguageCodeForm.Set3,
        "fra",
        LanguageNormalizationStatus.Success)]
    [TestCase(
        "aaa",
        LanguageCodeForm.Set3,
        LanguageCodeForm.Set3,
        "aaa",
        LanguageNormalizationStatus.AlreadyCanonical)]
    [TestCase(
        "gmw",
        LanguageCodeForm.Set5,
        LanguageCodeForm.Unknown,
        null,
        LanguageNormalizationStatus.IncompatibleEntity)]
    [TestCase(
        "fri",
        LanguageCodeForm.RetiredSet3,
        LanguageCodeForm.Set3,
        null,
        LanguageNormalizationStatus.IncompatibleEntity)]
    [TestCase(
        "iw",
        LanguageCodeForm.IanaLanguage,
        LanguageCodeForm.Alpha2,
        null,
        LanguageNormalizationStatus.IncompatibleEntity)]
    [TestCase(
        "qaa",
        LanguageCodeForm.PrivateUse,
        LanguageCodeForm.Unknown,
        null,
        LanguageNormalizationStatus.IncompatibleEntity)]
    [TestCase(
        "zzz",
        LanguageCodeForm.Unknown,
        LanguageCodeForm.Set3,
        null,
        LanguageNormalizationStatus.NotFound)]
    public void ConversionHandlesEveryReachableCodeSourceForm(
        string input,
        LanguageCodeForm sourceForm,
        LanguageCodeForm targetForm,
        string? output,
        LanguageNormalizationStatus status)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageNormalizationResult result = service.ConvertCode(
            input,
            new LanguageNormalizationOptions { TargetForm = targetForm });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(status));
            Assert.That(result.Output, Is.EqualTo(output));
            Assert.That(result.SourceForm, Is.EqualTo(sourceForm));
            Assert.That(
                result.TargetForm,
                Is.EqualTo(
                    targetForm == LanguageCodeForm.Unknown
                        ? sourceForm
                        : targetForm));
        });
    }

    [TestCase(
        LanguageCodeForm.Unknown,
        LanguageCodeForm.Set2Terminology,
        "eng",
        LanguageNormalizationStatus.AlreadyCanonical)]
    [TestCase(
        LanguageCodeForm.Alpha2,
        LanguageCodeForm.Alpha2,
        "en",
        LanguageNormalizationStatus.Success)]
    [TestCase(
        LanguageCodeForm.Set2Terminology,
        LanguageCodeForm.Set2Terminology,
        "eng",
        LanguageNormalizationStatus.AlreadyCanonical)]
    [TestCase(
        LanguageCodeForm.Set2Bibliographic,
        LanguageCodeForm.Set2Bibliographic,
        "eng",
        LanguageNormalizationStatus.AlreadyCanonical)]
    [TestCase(
        LanguageCodeForm.Set3,
        LanguageCodeForm.Set3,
        "eng",
        LanguageNormalizationStatus.AlreadyCanonical)]
    [TestCase(
        LanguageCodeForm.Set5,
        LanguageCodeForm.Set5,
        null,
        LanguageNormalizationStatus.TargetUnavailable)]
    [TestCase(
        LanguageCodeForm.RetiredSet3,
        LanguageCodeForm.RetiredSet3,
        null,
        LanguageNormalizationStatus.TargetUnavailable)]
    [TestCase(
        LanguageCodeForm.IanaLanguage,
        LanguageCodeForm.IanaLanguage,
        null,
        LanguageNormalizationStatus.TargetUnavailable)]
    [TestCase(
        LanguageCodeForm.IanaExtlang,
        LanguageCodeForm.IanaExtlang,
        null,
        LanguageNormalizationStatus.TargetUnavailable)]
    [TestCase(
        LanguageCodeForm.GrandfatheredTag,
        LanguageCodeForm.GrandfatheredTag,
        null,
        LanguageNormalizationStatus.TargetUnavailable)]
    [TestCase(
        LanguageCodeForm.RedundantTag,
        LanguageCodeForm.RedundantTag,
        null,
        LanguageNormalizationStatus.TargetUnavailable)]
    [TestCase(
        LanguageCodeForm.PrivateUse,
        LanguageCodeForm.PrivateUse,
        null,
        LanguageNormalizationStatus.TargetUnavailable)]
    public void ConversionReportsEveryTargetForm(
        LanguageCodeForm requestedForm,
        LanguageCodeForm resultForm,
        string? output,
        LanguageNormalizationStatus status)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageNormalizationResult result = service.ConvertCode(
            "eng",
            new LanguageNormalizationOptions { TargetForm = requestedForm });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(status));
            Assert.That(result.Output, Is.EqualTo(output));
            Assert.That(result.SourceForm, Is.EqualTo(LanguageCodeForm.Set2Terminology));
            Assert.That(result.TargetForm, Is.EqualTo(resultForm));
            Assert.That(
                result.AppliedSteps,
                Is.EqualTo(
                    status == LanguageNormalizationStatus.Success
                        ? new[] { LanguageNormalizationStep.ConvertCodeForm }
                        : Array.Empty<LanguageNormalizationStep>()));
        });
    }

    [Test]
    public void EveryBibliographicTerminologyPairConvertsInBothDirections()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageCodeEntry[] pairs = service.GetAllEntries()
            .Where(entry =>
                entry.Set2TerminologyCode != null &&
                entry.Set2BibliographicCode != null &&
                entry.Set2TerminologyCode != entry.Set2BibliographicCode)
            .ToArray();

        Assert.That(pairs, Has.Length.EqualTo(20));

        foreach (LanguageCodeEntry pair in pairs)
        {
            LanguageNormalizationResult terminology = service.ConvertCode(
                pair.Set2BibliographicCode,
                new LanguageNormalizationOptions
                {
                    TargetForm = LanguageCodeForm.Set2Terminology
                });
            LanguageNormalizationResult bibliographic = service.ConvertCode(
                pair.Set2TerminologyCode,
                new LanguageNormalizationOptions
                {
                    TargetForm = LanguageCodeForm.Set2Bibliographic
                });

            Assert.Multiple(() =>
            {
                Assert.That(
                    terminology.Status,
                    Is.EqualTo(LanguageNormalizationStatus.Success),
                    pair.Set2BibliographicCode);
                Assert.That(
                    terminology.Output,
                    Is.EqualTo(pair.Set2TerminologyCode),
                    pair.Set2BibliographicCode);
                Assert.That(
                    terminology.SourceForm,
                    Is.EqualTo(LanguageCodeForm.Set2Bibliographic),
                    pair.Set2BibliographicCode);
                Assert.That(
                    bibliographic.Status,
                    Is.EqualTo(LanguageNormalizationStatus.Success),
                    pair.Set2TerminologyCode);
                Assert.That(
                    bibliographic.Output,
                    Is.EqualTo(pair.Set2BibliographicCode),
                    pair.Set2TerminologyCode);
                Assert.That(
                    bibliographic.SourceForm,
                    Is.EqualTo(LanguageCodeForm.Set2Terminology),
                    pair.Set2TerminologyCode);
            });
        }
    }

    [Test]
    public void TrimmingAndCaseNormalizationAreIndependentPublicPolicies()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageNormalizationResult neither = service.NormalizeCode(
            "  ENG  ",
            new LanguageNormalizationOptions());
        LanguageNormalizationResult caseOnly = service.NormalizeCode(
            "ENG",
            new LanguageNormalizationOptions { NormalizeCase = true });
        LanguageNormalizationResult trimOnly = service.NormalizeCode(
            "  eng  ",
            new LanguageNormalizationOptions { TrimWhitespace = true });
        LanguageNormalizationResult both = service.NormalizeCode(
            "  ENG  ",
            new LanguageNormalizationOptions
            {
                TrimWhitespace = true,
                NormalizeCase = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(neither.Status, Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(caseOnly.Output, Is.EqualTo("eng"));
            Assert.That(caseOnly.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.NormalizeCase }));
            Assert.That(trimOnly.Output, Is.EqualTo("eng"));
            Assert.That(trimOnly.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.TrimWhitespace }));
            Assert.That(both.Output, Is.EqualTo("eng"));
            Assert.That(both.AppliedSteps, Is.EqualTo(new[]
            {
                LanguageNormalizationStep.TrimWhitespace,
                LanguageNormalizationStep.NormalizeCase
            }));
        });
    }

    [Test]
    public void AliasRetirementAndMacrolanguagePoliciesRemainIndependent()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageNormalizationResult aliasPreserved = service.NormalizeCode(
            "iw",
            new LanguageNormalizationOptions());
        LanguageNormalizationResult aliasReplaced = service.NormalizeCode(
            "iw",
            new LanguageNormalizationOptions { ReplaceDeprecated = true });
        LanguageNormalizationResult retiredPreserved = service.NormalizeCode(
            "fri",
            new LanguageNormalizationOptions());
        LanguageNormalizationResult retiredReplaced = service.NormalizeCode(
            "fri",
            new LanguageNormalizationOptions { ReplaceRetired = true });
        LanguageNormalizationResult memberPreserved = service.NormalizeCode(
            "aao",
            new LanguageNormalizationOptions
            {
                MacrolanguagePolicy = MacrolanguageNormalizationPolicy.Preserve
            });
        LanguageNormalizationResult memberFolded = service.NormalizeCode(
            "aao",
            new LanguageNormalizationOptions
            {
                MacrolanguagePolicy = MacrolanguageNormalizationPolicy.FoldToMacrolanguage
            });

        Assert.Multiple(() =>
        {
            Assert.That(aliasPreserved.Output, Is.EqualTo("iw"));
            Assert.That(aliasPreserved.AppliedSteps, Is.Empty);
            Assert.That(aliasReplaced.Output, Is.EqualTo("he"));
            Assert.That(aliasReplaced.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ReplaceDeprecated }));
            Assert.That(retiredPreserved.Output, Is.EqualTo("fri"));
            Assert.That(retiredPreserved.AppliedSteps, Is.Empty);
            Assert.That(retiredReplaced.Output, Is.EqualTo("fry"));
            Assert.That(retiredReplaced.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ReplaceRetired }));
            Assert.That(memberPreserved.Output, Is.EqualTo("aao"));
            Assert.That(memberPreserved.MacrolanguageRelationship, Is.Null);
            Assert.That(memberFolded.Output, Is.EqualTo("ara"));
            Assert.That(memberFolded.MacrolanguageRelationship?.ChildCode, Is.EqualTo("aao"));
            Assert.That(memberFolded.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.FoldMacrolanguage }));
        });
    }

    [Test]
    public void MissingMappingsAreReportedWithoutInventingCodes()
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageNormalizationResult missingAlpha2 = service.ConvertCode(
            "aaa",
            new LanguageNormalizationOptions { TargetForm = LanguageCodeForm.Alpha2 });
        LanguageNormalizationResult groupSource = service.ConvertCode(
            "gmw",
            new LanguageNormalizationOptions { TargetForm = LanguageCodeForm.Set3 });

        Assert.Multiple(() =>
        {
            Assert.That(missingAlpha2.Status, Is.EqualTo(
                LanguageNormalizationStatus.TargetUnavailable));
            Assert.That(missingAlpha2.Output, Is.Null);
            Assert.That(missingAlpha2.Entry?.Set3Code, Is.EqualTo("aaa"));
            Assert.That(groupSource.Status, Is.EqualTo(
                LanguageNormalizationStatus.IncompatibleEntity));
            Assert.That(groupSource.Output, Is.Null);
            Assert.That(groupSource.Entry?.Set5Code, Is.EqualTo("gmw"));
        });
    }

    [Test]
    public void ComplexNormalizationHasStableStepOrderAndIsIdempotent()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageNormalizationOptions options = new LanguageNormalizationOptions
        {
            TargetForm = LanguageCodeForm.Alpha2,
            ReplaceDeprecated = true,
            ReplaceRetired = true,
            MacrolanguagePolicy = MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
            TrimWhitespace = true,
            NormalizeCase = true
        };

        LanguageNormalizationResult first = service.NormalizeCode("  AJP  ", options);
        LanguageNormalizationResult second = service.NormalizeCode(first.Output, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(first.Output, Is.EqualTo("ar"));
            Assert.That(first.SourceForm, Is.EqualTo(LanguageCodeForm.IanaLanguage));
            Assert.That(first.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(first.Entry?.Set3Code, Is.EqualTo("ara"));
            Assert.That(first.AppliedSteps, Is.EqualTo(new[]
            {
                LanguageNormalizationStep.TrimWhitespace,
                LanguageNormalizationStep.NormalizeCase,
                LanguageNormalizationStep.ReplaceDeprecated,
                LanguageNormalizationStep.FoldMacrolanguage,
                LanguageNormalizationStep.ConvertCodeForm
            }));
            Assert.That(second.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(second.Output, Is.EqualTo(first.Output));
            Assert.That(second.TargetForm, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(second.AppliedSteps, Is.Empty);
        });
    }
}
