using languagecodes;

namespace languagecodes.Tests;

public class LanguageTagNormalizationEngineTests
{
    [Test]
    public void NormalizesWhitespaceSeparatorsAndStructuralCasingInOrder()
    {
        LanguageNormalizationOptions options = new LanguageNormalizationOptions
        {
            TrimWhitespace = true,
            NormalizeTagSeparators = true,
            NormalizeTagCasing = true
        };
        LanguageNormalizationResult result = LanguageNormalizationEngine.NormalizeTag(
            "  EN_US-u-CA-GREGORY-x-Private  ",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.OriginalInput, Is.EqualTo(
                "  EN_US-u-CA-GREGORY-x-Private  "));
            Assert.That(result.Output, Is.EqualTo("en-US-u-ca-gregory-x-private"));
            Assert.That(result.AppliedSteps, Is.EqualTo(new[]
            {
                LanguageNormalizationStep.TrimWhitespace,
                LanguageNormalizationStep.NormalizeSeparators,
                LanguageNormalizationStep.NormalizeTagCasing
            }));
        });
    }

    [Test]
    public void SeparatorAndCasingPoliciesAreIndependent()
    {
        LanguageNormalizationResult invalid = LanguageNormalizationEngine.NormalizeTag(
            "EN_US",
            new LanguageNormalizationOptions
            {
                NormalizeTagCasing = true
            });
        LanguageNormalizationResult preserved = LanguageNormalizationEngine.NormalizeTag(
            "EN-us",
            new LanguageNormalizationOptions());
        LanguageNormalizationResult separated = LanguageNormalizationEngine.NormalizeTag(
            "EN_us",
            new LanguageNormalizationOptions
            {
                NormalizeTagSeparators = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status, Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(invalid.Output, Is.Null);
            Assert.That(preserved.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(preserved.Output, Is.EqualTo("EN-us"));
            Assert.That(separated.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(separated.Output, Is.EqualTo("EN-us"));
            Assert.That(separated.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.NormalizeSeparators }));
        });
    }

    [TestCase("IW-bu", "he-MM")]
    [TestCase("ja-Latn-hepburn-heploc", "ja-Latn-hepburn-alalc97")]
    [TestCase("SGN-us", "ase")]
    public void AppliesIanaPreferredValuesWhenRequested(
        string value,
        string expected)
    {
        LanguageNormalizationResult result = LanguageNormalizationEngine.NormalizeTag(
            value,
            new LanguageNormalizationOptions
            {
                ReplaceDeprecated = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.Output, Is.EqualTo(expected));
            Assert.That(result.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ReplaceSubtag }));
        });
    }

    [Test]
    public void LeavesDeprecatedSubtagsInPlaceWhenReplacementIsDisabled()
    {
        LanguageNormalizationResult result = LanguageNormalizationEngine.NormalizeTag(
            "iw-BU",
            new LanguageNormalizationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(result.Output, Is.EqualTo("iw-BU"));
            Assert.That(result.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void ReplacesGrandfatheredTagsOnlyUnderTheirDedicatedPolicy()
    {
        LanguageNormalizationResult retained = LanguageNormalizationEngine.NormalizeTag(
            "I-KLINGON",
            new LanguageNormalizationOptions
            {
                NormalizeTagCasing = true
            });
        LanguageNormalizationResult replaced = LanguageNormalizationEngine.NormalizeTag(
            "I-KLINGON",
            new LanguageNormalizationOptions
            {
                ReplaceGrandfatheredTags = true
            });
        LanguageNormalizationResult withoutPreferred = LanguageNormalizationEngine.NormalizeTag(
            "i-enochian",
            new LanguageNormalizationOptions
            {
                ReplaceGrandfatheredTags = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(retained.Output, Is.EqualTo("i-klingon"));
            Assert.That(retained.SourceForm, Is.EqualTo(LanguageCodeForm.GrandfatheredTag));
            Assert.That(retained.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.NormalizeTagCasing }));
            Assert.That(replaced.Output, Is.EqualTo("tlh"));
            Assert.That(replaced.TargetForm, Is.EqualTo(LanguageCodeForm.IanaLanguage));
            Assert.That(replaced.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ReplaceGrandfathered }));
            Assert.That(withoutPreferred.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(withoutPreferred.Output, Is.EqualTo("i-enochian"));
        });
    }

    [TestCase("eng-US", "en-US")]
    [TestCase("fra-Latn", "fr-Latn")]
    [TestCase("deu-a-example", "de-a-example")]
    public void ShortensIsoLanguageCodesThatHaveAlpha2Forms(
        string value,
        string expected)
    {
        LanguageNormalizationResult result = LanguageNormalizationEngine.NormalizeTag(
            value,
            new LanguageNormalizationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.Output, Is.EqualTo(expected));
            Assert.That(result.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ReplaceSubtag }));
        });
    }

    [Test]
    public void CanonicalizesExtlangFormWithoutChangingTrailingPayloads()
    {
        LanguageNormalizationResult result = LanguageNormalizationEngine.NormalizeTag(
            "ZH-cmn-Hans-CN-a-ABC-x-Private",
            new LanguageNormalizationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.Output, Is.EqualTo("cmn-Hans-CN-a-ABC-x-Private"));
            Assert.That(result.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ReplaceSubtag }));
        });
    }

    [Test]
    public void RemovesSuppressScriptOnlyWhenExplicitlyRequested()
    {
        LanguageNormalizationResult retained = LanguageNormalizationEngine.NormalizeTag(
            "en-Latn-US-a-example-x-private",
            new LanguageNormalizationOptions());
        LanguageNormalizationResult removed = LanguageNormalizationEngine.NormalizeTag(
            "en-Latn-US-a-example-x-private",
            new LanguageNormalizationOptions
            {
                RemoveSuppressScript = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(retained.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(retained.Output, Is.EqualTo(
                "en-Latn-US-a-example-x-private"));
            Assert.That(removed.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(removed.Output, Is.EqualTo("en-US-a-example-x-private"));
            Assert.That(removed.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.RemoveSuppressScript }));
        });
    }

    [Test]
    public void DistinguishesMalformedUnassignedAndRegistryInvalidTags()
    {
        LanguageNormalizationResult malformed = LanguageNormalizationEngine.NormalizeTag(
            "en--US",
            new LanguageNormalizationOptions());
        LanguageNormalizationResult unassigned = LanguageNormalizationEngine.NormalizeTag(
            "abcde-US",
            new LanguageNormalizationOptions());
        LanguageNormalizationResult prefixMismatch = LanguageNormalizationEngine.NormalizeTag(
            "en-aao",
            new LanguageNormalizationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Status, Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(unassigned.Status, Is.EqualTo(LanguageNormalizationStatus.NotFound));
            Assert.That(prefixMismatch.Status, Is.EqualTo(
                LanguageNormalizationStatus.Invalid));
            Assert.That(malformed.Output, Is.Null);
            Assert.That(unassigned.Output, Is.Null);
            Assert.That(prefixMismatch.Output, Is.Null);
        });
    }

    [Test]
    public void SuccessfulNormalizationIsIdempotent()
    {
        LanguageNormalizationOptions options = new LanguageNormalizationOptions
        {
            NormalizeTagSeparators = true,
            NormalizeTagCasing = true,
            ReplaceDeprecated = true,
            ReplaceGrandfatheredTags = true,
            RemoveSuppressScript = true
        };
        LanguageNormalizationResult first = LanguageNormalizationEngine.NormalizeTag(
            "EN_latn-BU-a-ABC-x-Private",
            options);
        LanguageNormalizationResult second = LanguageNormalizationEngine.NormalizeTag(
            first.Output,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(first.Output, Is.EqualTo("en-MM-a-abc-x-private"));
            Assert.That(second.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(second.Output, Is.EqualTo(first.Output));
            Assert.That(second.AppliedSteps, Is.Empty);
        });
    }
}
