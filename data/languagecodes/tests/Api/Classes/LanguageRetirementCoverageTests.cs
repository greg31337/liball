using languagecodes;

namespace languagecodes.Tests;

public sealed class LanguageRetirementCoverageTests
{
    [TestCase(
        "fri",
        "Western Frisian",
        LanguageRetirementReason.Change,
        "fry",
        null,
        "2005-11-16")]
    [TestCase(
        "dat",
        "Darang Deng",
        LanguageRetirementReason.Duplicate,
        "mhu",
        null,
        "2008-01-14")]
    [TestCase(
        "aay",
        "Aariya",
        LanguageRetirementReason.Nonexistent,
        "",
        null,
        "2009-01-16")]
    [TestCase(
        "agp",
        "Paranan",
        LanguageRetirementReason.Split,
        "apf,prf",
        "Split into Pahanan Agta [apf] and Paranan [prf] (new identifier)",
        "2010-01-18")]
    [TestCase(
        "dit",
        "Dirari",
        LanguageRetirementReason.Merge,
        "dif",
        null,
        "2019-01-25")]
    public void EveryRetirementReasonReturnsAuthoritativeMetadata(
        string code,
        string formerName,
        LanguageRetirementReason reason,
        string replacementCodes,
        string? remedy,
        string effectiveDate)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageRetirement? retirement = service.GetRetirement(code);

        Assert.Multiple(() =>
        {
            Assert.That(retirement, Is.Not.Null);
            Assert.That(retirement?.RetiredCode, Is.EqualTo(code));
            Assert.That(retirement?.FormerReferenceName, Is.EqualTo(formerName));
            Assert.That(retirement?.Reason, Is.EqualTo(reason));
            Assert.That(
                string.Join(",", retirement?.ReplacementCodes ?? Array.Empty<string>()),
                Is.EqualTo(replacementCodes));
            Assert.That(retirement?.Remedy, Is.EqualTo(remedy));
            Assert.That(retirement?.EffectiveDate, Is.EqualTo(effectiveDate));
        });
    }

    [TestCase("fri", LanguageRetirementReason.Change, "fry")]
    [TestCase("dat", LanguageRetirementReason.Duplicate, "mhu")]
    [TestCase("aay", LanguageRetirementReason.Nonexistent, "")]
    [TestCase("agp", LanguageRetirementReason.Split, "apf,prf")]
    [TestCase("dit", LanguageRetirementReason.Merge, "dif")]
    public void RetiredCodeLookupsEmbedTheSameRetirementMapping(
        string code,
        LanguageRetirementReason reason,
        string replacementCodes)
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageLookupOptions options = new LanguageLookupOptions
        {
            IncludeRetiredCodes = true
        };

        LanguageLookupResult lookup = service.LookupCode(
            code,
            LanguageCodeForm.RetiredSet3,
            options);
        LanguageRetirement? direct = service.GetRetirement(code);

        Assert.Multiple(() =>
        {
            Assert.That(lookup.Status, Is.EqualTo(LanguageLookupStatus.Retired));
            Assert.That(lookup.Entry?.Set3Code, Is.EqualTo(code));
            Assert.That(lookup.Entry?.Status, Is.EqualTo(LanguageCodeStatus.Retired));
            Assert.That(lookup.Entry?.Retirement?.Reason, Is.EqualTo(reason));
            Assert.That(
                string.Join(",", lookup.Entry?.Retirement?.ReplacementCodes ??
                    Array.Empty<string>()),
                Is.EqualTo(replacementCodes));
            Assert.That(lookup.Entry?.Retirement?.Remedy, Is.EqualTo(direct?.Remedy));
            Assert.That(
                lookup.Entry?.Retirement?.EffectiveDate,
                Is.EqualTo(direct?.EffectiveDate));
        });
    }

    [TestCase("fri", "fry")]
    [TestCase("dat", "mhu")]
    [TestCase("dit", "dif")]
    public void SingularRetirementsResolveToTheirAuthoritativeActiveCode(
        string retiredCode,
        string activeCode)
    {
        LanguageCodeService service = new LanguageCodeService();

        LanguageNormalizationResult result = service.NormalizeCode(
            retiredCode,
            new LanguageNormalizationOptions { ReplaceRetired = true });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.Output, Is.EqualTo(activeCode));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo(activeCode));
            Assert.That(result.Entry?.Status, Is.EqualTo(LanguageCodeStatus.Active));
            Assert.That(result.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ReplaceRetired }));
            Assert.That(result.ReplacementCandidates, Is.Empty);
        });
    }

    [Test]
    public void SplitAndNoReplacementRetirementsRemainExplicit()
    {
        LanguageCodeService service = new LanguageCodeService();
        LanguageNormalizationOptions options = new LanguageNormalizationOptions
        {
            ReplaceRetired = true
        };

        LanguageNormalizationResult split = service.NormalizeCode("agp", options);
        LanguageNormalizationResult nonexistent = service.NormalizeCode("aay", options);

        Assert.Multiple(() =>
        {
            Assert.That(split.Status, Is.EqualTo(LanguageNormalizationStatus.Split));
            Assert.That(split.Output, Is.Null);
            Assert.That(split.ReplacementCandidates, Is.EqualTo(new[] { "apf", "prf" }));
            Assert.That(split.Entry?.Retirement?.Remedy, Is.EqualTo(
                "Split into Pahanan Agta [apf] and Paranan [prf] (new identifier)"));
            Assert.That(nonexistent.Status, Is.EqualTo(
                LanguageNormalizationStatus.NoReplacement));
            Assert.That(nonexistent.Output, Is.Null);
            Assert.That(nonexistent.ReplacementCandidates, Is.Empty);
            Assert.That(nonexistent.Entry?.Retirement?.Reason, Is.EqualTo(
                LanguageRetirementReason.Nonexistent));
        });
    }

    [Test]
    public void ActiveUnknownAndNonCanonicalInputsHaveNoDirectRetirementRecord()
    {
        LanguageCodeService service = new LanguageCodeService();

        Assert.Multiple(() =>
        {
            Assert.That(service.GetRetirement("eng"), Is.Null);
            Assert.That(service.GetRetirement("zzz"), Is.Null);
            Assert.That(service.GetRetirement("FRI"), Is.Null);
            Assert.That(service.GetRetirement(string.Empty), Is.Null);
            Assert.That(service.GetRetirement(null), Is.Null);
        });
    }
}
