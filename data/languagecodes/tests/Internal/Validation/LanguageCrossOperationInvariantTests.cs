using languagecodes;

namespace languagecodes.Tests;

public class LanguageCrossOperationInvariantTests
{
    [TestCase("fr", LanguageCodeForm.Alpha2)]
    [TestCase("fr", LanguageCodeForm.Set2Terminology)]
    [TestCase("fr", LanguageCodeForm.Set2Bibliographic)]
    [TestCase("fr", LanguageCodeForm.Set3)]
    [TestCase("afa", LanguageCodeForm.Unknown)]
    public void SuccessfulConversionsValidateInTheirReportedTargetForm(
        string code,
        LanguageCodeForm targetForm)
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.ConvertCode(
                code,
                new LanguageNormalizationOptions
                {
                    TargetForm = targetForm
                });

        Assert.Multiple(() =>
        {
            Assert.That(IsSuccessful(result.Status), Is.True);
            Assert.That(result.Output, Is.Not.Null);
            Assert.That(
                LanguageValidationEngine.IsNormalizationOutputValid(result),
                Is.True);
        });
    }

    [Test]
    public void SuccessfulComposedNormalizationsValidateAcrossOutputCategories()
    {
        LanguageNormalizationResult active =
            LanguageNormalizationEngine.NormalizeCode(
                "eng",
                CreateOptions(LanguageCodeForm.Alpha2, true, true));
        LanguageNormalizationResult retired =
            LanguageNormalizationEngine.NormalizeCode(
                "fri",
                CreateOptions(LanguageCodeForm.Alpha2, true, true));
        LanguageNormalizationResult groupFallback =
            LanguageNormalizationEngine.NormalizeCode(
                "afa",
                CreateOptions(LanguageCodeForm.Set3, false, false));
        LanguageNormalizationResult privateUse =
            LanguageNormalizationEngine.NormalizeCode(
                "qaa",
                CreateOptions(LanguageCodeForm.Unknown, false, false));
        LanguageNormalizationResult splitPreserved =
            LanguageNormalizationEngine.NormalizeCode(
                "agp",
                CreateOptions(LanguageCodeForm.Unknown, false, false));

        LanguageNormalizationResult[] results =
        {
            active,
            retired,
            groupFallback,
            privateUse,
            splitPreserved
        };

        Assert.Multiple(() =>
        {
            foreach (LanguageNormalizationResult result in results)
            {
                Assert.That(IsSuccessful(result.Status), Is.True);
                Assert.That(result.Output, Is.Not.Null);
                Assert.That(
                    LanguageValidationEngine.IsNormalizationOutputValid(result),
                    Is.True);
            }

            Assert.That(groupFallback.Output, Is.EqualTo("afa"));
            Assert.That(
                groupFallback.TargetForm,
                Is.EqualTo(LanguageCodeForm.Set2Terminology));
        });
    }

    [Test]
    public void InvariantGuardRejectsAClaimedSuccessWithAnInvalidTargetOutput()
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.EnforceSuccessfulCodeNormalizationResult(
                new LanguageNormalizationResult
                {
                    Status = LanguageNormalizationStatus.Success,
                    OriginalInput = "eng",
                    Output = "zzz",
                    TargetForm = LanguageCodeForm.Set3,
                    Entry = new LanguageCodeEntry
                    {
                        Set3Code = "eng",
                        InSet3 = true,
                        Scope = LanguageScope.Individual,
                        LanguageType = LanguageType.Living,
                        Status = LanguageCodeStatus.Active
                    }
                });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(
                LanguageNormalizationStatus.IncompatibleEntity));
            Assert.That(result.OriginalInput, Is.EqualTo("eng"));
            Assert.That(result.Output, Is.Null);
            Assert.That(result.Entry, Is.Null);
        });
    }

    [Test]
    public void FailedValidationNeverReturnsAResolvedEntry()
    {
        LanguageValidationResult wrongSet = LanguageValidationEngine.ValidateCode(
            "eng",
            new LanguageValidationOptions
            {
                RequiredSet = Iso639Set.Set1
            });
        LanguageValidationResult strictAlias = LanguageValidationEngine.ValidateCode(
            "eng",
            new LanguageValidationOptions
            {
                RequireCanonical = true
            });
        LanguageValidationResult rejectedMacro = LanguageValidationEngine.ValidateCode(
            "ara",
            new LanguageValidationOptions());
        LanguageValidationResult rejectedGroup = LanguageValidationEngine.ValidateCode(
            "afa",
            new LanguageValidationOptions());
        LanguageValidationResult rejectedSpecial = LanguageValidationEngine.ValidateCode(
            "mis",
            new LanguageValidationOptions());

        LanguageValidationResult[] results =
        {
            wrongSet,
            strictAlias,
            rejectedMacro,
            rejectedGroup,
            rejectedSpecial
        };

        Assert.Multiple(() =>
        {
            foreach (LanguageValidationResult result in results)
            {
                Assert.That(result.IsWellFormed, Is.True);
                Assert.That(result.IsAssigned, Is.True);
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Entry, Is.Null);
            }

            Assert.That(strictAlias.CanonicalValue, Is.EqualTo("en"));
        });
    }

    private static bool IsSuccessful(LanguageNormalizationStatus status)
    {
        return status == LanguageNormalizationStatus.Success ||
            status == LanguageNormalizationStatus.AlreadyCanonical;
    }

    private static LanguageNormalizationOptions CreateOptions(
        LanguageCodeForm targetForm,
        bool replaceDeprecated,
        bool replaceRetired)
    {
        return new LanguageNormalizationOptions
        {
            TargetForm = targetForm,
            ReplaceDeprecated = replaceDeprecated,
            ReplaceRetired = replaceRetired
        };
    }
}
