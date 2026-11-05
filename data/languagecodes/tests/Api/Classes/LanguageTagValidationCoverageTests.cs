using languagecodes;

namespace languagecodes.Tests;

public sealed class LanguageTagValidationCoverageTests
{
    [TestCase("en-Latn-US-fonipa-u-ca-gregory")]
    [TestCase("cmn")]
    [TestCase("i-default")]
    [TestCase("de-CH-1901")]
    [TestCase("sl-rozaj-biske-1994")]
    public void AssignedRegistryComponentsValidate(string value)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageValidationResult result = service.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues.Any(issue =>
                issue.Error == LanguageValidationError.UnassignedCode), Is.False);
            Assert.That(result.Issues.Any(issue =>
                issue.Error == LanguageValidationError.PrefixMismatch), Is.False);
        });
    }

    [TestCase("abcd", "abcd")]
    [TestCase("en-zzz", "zzz")]
    [TestCase("en-Abcd", "Abcd")]
    [TestCase("en-AB", "AB")]
    [TestCase("en-abcde", "abcde")]
    public void UnassignedRegistryComponentsIdentifyTheOffendingSubtag(
        string value,
        string component)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageValidationResult result = service.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Entry, Is.Null);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.UnassignedCode));
            Assert.That(result.Issues[0].Component, Is.EqualTo(component));
        });
    }

    [TestCase("iw-BU", "he-MM")]
    [TestCase("i-klingon", "tlh")]
    [TestCase("zh-cmn", "cmn")]
    public void DeprecatedAndNoncanonicalTagsReportPreferredValues(
        string value,
        string canonicalValue)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageValidationResult warning = service.ValidateTag(
            value,
            new LanguageValidationOptions());
        LanguageValidationResult strict = service.ValidateTag(
            value,
            new LanguageValidationOptions { RequireCanonical = true });

        Assert.Multiple(() =>
        {
            Assert.That(warning.IsWellFormed, Is.True);
            Assert.That(warning.IsAssigned, Is.True);
            Assert.That(warning.IsValid, Is.True);
            Assert.That(warning.IsCanonical, Is.False);
            Assert.That(warning.CanonicalValue, Is.EqualTo(canonicalValue));
            Assert.That(warning.Issues.Select(issue => issue.Error), Is.EqualTo(
                new[] { LanguageValidationError.NonCanonical }));
            Assert.That(warning.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Warning));
            Assert.That(strict.IsValid, Is.False);
            Assert.That(strict.CanonicalValue, Is.EqualTo(canonicalValue));
            Assert.That(strict.Issues[0].Severity, Is.EqualTo(
                LanguageValidationSeverity.Error));
        });
    }

    [TestCase("ar-aao")]
    [TestCase("de-CH-1901")]
    [TestCase("sl-rozaj-biske-1994")]
    [TestCase("sl-IT-rozaj-biske-1994")]
    public void RegisteredExtlangAndVariantPrefixesAreAccepted(string value)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageValidationResult result = service.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Issues.Any(issue =>
                issue.Error == LanguageValidationError.PrefixMismatch), Is.False);
        });
    }

    [TestCase("en-aao", "aao")]
    [TestCase("en-1994", "1994")]
    [TestCase("zh-cmn-yue", "yue")]
    public void ExtlangAndVariantPrefixMismatchesAreRejected(
        string value,
        string component)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageValidationResult result = service.ValidateTag(
            value,
            new LanguageValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsWellFormed, Is.True);
            Assert.That(result.IsAssigned, Is.True);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Error, Is.EqualTo(
                LanguageValidationError.PrefixMismatch));
            Assert.That(result.Issues[0].Component, Is.EqualTo(component));
        });
    }

    [TestCase("IW-bu", "he-MM")]
    [TestCase("ja-Latn-hepburn-heploc", "ja-Latn-hepburn-alalc97")]
    [TestCase("SGN-us", "ase")]
    public void PreferredRegistryValuesAreAppliedOnlyWhenRequested(
        string value,
        string preferredValue)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageNormalizationResult retained = service.NormalizeTag(
            value,
            new LanguageNormalizationOptions());
        LanguageNormalizationResult replaced = service.NormalizeTag(
            value,
            new LanguageNormalizationOptions { ReplaceDeprecated = true });

        Assert.Multiple(() =>
        {
            Assert.That(retained.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(retained.Output, Is.EqualTo(value));
            Assert.That(retained.AppliedSteps, Is.Empty);
            Assert.That(replaced.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(replaced.Output, Is.EqualTo(preferredValue));
            Assert.That(replaced.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ReplaceSubtag }));
        });
    }

    [TestCase("eng-US", "en-US")]
    [TestCase("fra-Latn", "fr-Latn")]
    [TestCase("deu-a-example", "de-a-example")]
    public void OverlongIsoLanguageCodesShortenToAlpha2(
        string value,
        string expected)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageNormalizationResult result = service.NormalizeTag(
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
    public void SuppressScriptRemovalRequiresItsExplicitPolicy()
    {
        LanguageCodeService service = new LanguageCodeService();
        const string value = "en-Latn-US-a-example-x-private";

        LanguageNormalizationResult retained = service.NormalizeTag(
            value,
            new LanguageNormalizationOptions());
        LanguageNormalizationResult removed = service.NormalizeTag(
            value,
            new LanguageNormalizationOptions { RemoveSuppressScript = true });

        Assert.Multiple(() =>
        {
            Assert.That(retained.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(retained.Output, Is.EqualTo(value));
            Assert.That(retained.AppliedSteps, Is.Empty);
            Assert.That(removed.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(removed.Output, Is.EqualTo("en-US-a-example-x-private"));
            Assert.That(removed.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.RemoveSuppressScript }));
        });
    }

    [Test]
    public void PrivateUsePolicyControlsTagsAndSuffixes()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageValidationOptions allowed = new LanguageValidationOptions
        {
            AllowPrivateUseCodes = true
        };

        LanguageValidationResult rejected = service.ValidateTag(
            "x-company-internal",
            new LanguageValidationOptions());
        LanguageValidationResult privateOnly = service.ValidateTag(
            "x-company-internal",
            allowed);
        LanguageValidationResult privateSuffix = service.ValidateTag(
            "en-US-x-company",
            allowed);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.IsValid, Is.False);
            Assert.That(rejected.Issues.All(issue =>
                issue.Error == LanguageValidationError.PrivateUseNotAllowed), Is.True);
            Assert.That(privateOnly.IsWellFormed, Is.True);
            Assert.That(privateOnly.IsAssigned, Is.False);
            Assert.That(privateOnly.IsValid, Is.True);
            Assert.That(privateOnly.Issues.All(issue =>
                issue.Error == LanguageValidationError.ReservedCode), Is.True);
            Assert.That(privateSuffix.IsWellFormed, Is.True);
            Assert.That(privateSuffix.IsAssigned, Is.False);
            Assert.That(privateSuffix.IsValid, Is.True);
            Assert.That(privateSuffix.Entry?.Set3Code, Is.EqualTo("eng"));
        });
    }

    [Test]
    public void CombinedCanonicalizationPreservesExtensionsAndIsIdempotent()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageNormalizationOptions options = new LanguageNormalizationOptions
        {
            TrimWhitespace = true,
            NormalizeTagSeparators = true,
            NormalizeTagCasing = true,
            ReplaceDeprecated = true,
            ReplaceGrandfatheredTags = true,
            RemoveSuppressScript = true
        };
        const string input = "  EN_latn-BU-u-CA-GREGORY-x-Private  ";

        LanguageNormalizationResult first = service.NormalizeTag(input, options);
        LanguageNormalizationResult second = service.NormalizeTag(first.Output, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(first.Output, Is.EqualTo("en-MM-u-ca-gregory-x-private"));
            Assert.That(first.Entry?.Set3Code, Is.EqualTo("eng"));
            Assert.That(first.AppliedSteps, Is.EqualTo(new[]
            {
                LanguageNormalizationStep.TrimWhitespace,
                LanguageNormalizationStep.NormalizeSeparators,
                LanguageNormalizationStep.ReplaceSubtag,
                LanguageNormalizationStep.RemoveSuppressScript,
                LanguageNormalizationStep.NormalizeTagCasing
            }));
            Assert.That(second.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(second.Output, Is.EqualTo(first.Output));
            Assert.That(second.AppliedSteps, Is.Empty);
        });
    }
}
