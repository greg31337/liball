using languagecodes;

namespace languagecodes.Tests;

public class LanguageHistoricalNormalizationEngineTests
{
    [Test]
    public void ActiveAndPolicyExcludedHistoricalCodesRemainUnchanged()
    {
        LanguageNormalizationResult active =
            LanguageNormalizationEngine.NormalizeHistoricalCode(
                "eng",
                CreateOptions(true, true, false, true));
        LanguageNormalizationResult retired =
            LanguageNormalizationEngine.NormalizeHistoricalCode(
                "fri",
                CreateOptions(false, false, false, true));
        LanguageNormalizationResult deprecated =
            LanguageNormalizationEngine.NormalizeHistoricalCode(
                "iw",
                CreateOptions(false, false, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(active.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(active.Output, Is.EqualTo("eng"));
            Assert.That(active.AppliedSteps, Is.Empty);
            Assert.That(retired.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(retired.Output, Is.EqualTo("fri"));
            Assert.That(retired.SourceForm, Is.EqualTo(LanguageCodeForm.RetiredSet3));
            Assert.That(retired.TargetForm, Is.EqualTo(LanguageCodeForm.RetiredSet3));
            Assert.That(retired.AppliedSteps, Is.Empty);
            Assert.That(deprecated.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(deprecated.Output, Is.EqualTo("iw"));
            Assert.That(deprecated.SourceForm, Is.EqualTo(LanguageCodeForm.IanaLanguage));
            Assert.That(deprecated.AppliedSteps, Is.Empty);
        });
    }

    [TestCase("fri", "fry", LanguageCodeForm.RetiredSet3)]
    [TestCase("asd", "snz", LanguageCodeForm.IanaLanguage)]
    [TestCase("pmc", "huw", LanguageCodeForm.IanaLanguage)]
    public void SingularRetirementsUseTheirAuthoritativeReplacement(
        string retiredCode,
        string expectedReplacement,
        LanguageCodeForm expectedSourceForm)
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeHistoricalCode(
                retiredCode,
                CreateOptions(false, true, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.OriginalInput, Is.EqualTo(retiredCode));
            Assert.That(result.Output, Is.EqualTo(expectedReplacement));
            Assert.That(result.SourceForm, Is.EqualTo(expectedSourceForm));
            Assert.That(result.TargetForm, Is.EqualTo(LanguageCodeForm.Set3));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo(expectedReplacement));
            Assert.That(result.Entry?.Status, Is.EqualTo(LanguageCodeStatus.Active));
            Assert.That(result.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ReplaceRetired }));
            Assert.That(result.ReplacementCandidates, Is.Empty);
        });
    }

    [TestCase("iw", "he", "heb", LanguageCodeForm.Alpha2)]
    [TestCase("in", "id", "ind", LanguageCodeForm.Alpha2)]
    [TestCase("adp", "dz", "dzo", LanguageCodeForm.Alpha2)]
    public void DeprecatedIanaCodesUseTheirOwnAuthoritativePreferredValue(
        string deprecatedCode,
        string expectedOutput,
        string expectedSet3Code,
        LanguageCodeForm expectedTargetForm)
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeHistoricalCode(
                deprecatedCode,
                CreateOptions(true, true, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.Output, Is.EqualTo(expectedOutput));
            Assert.That(result.SourceForm, Is.EqualTo(LanguageCodeForm.IanaLanguage));
            Assert.That(result.TargetForm, Is.EqualTo(expectedTargetForm));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo(expectedSet3Code));
            Assert.That(result.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ReplaceDeprecated }));
        });
    }

    [Test]
    public void DeprecatedAndRetiredPoliciesAreIndependent()
    {
        LanguageNormalizationResult deprecatedIgnored =
            LanguageNormalizationEngine.NormalizeHistoricalCode(
                "iw",
                CreateOptions(false, true, false, true));
        LanguageNormalizationResult retiredIgnored =
            LanguageNormalizationEngine.NormalizeHistoricalCode(
                "fri",
                CreateOptions(true, false, false, true));
        LanguageNormalizationResult overlappingRetirement =
            LanguageNormalizationEngine.NormalizeHistoricalCode(
                "adp",
                CreateOptions(false, true, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(deprecatedIgnored.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(deprecatedIgnored.Output, Is.EqualTo("iw"));
            Assert.That(retiredIgnored.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(retiredIgnored.Output, Is.EqualTo("fri"));
            Assert.That(overlappingRetirement.Status, Is.EqualTo(
                LanguageNormalizationStatus.Success));
            Assert.That(overlappingRetirement.Output, Is.EqualTo("dzo"));
            Assert.That(overlappingRetirement.TargetForm, Is.EqualTo(LanguageCodeForm.Set3));
            Assert.That(overlappingRetirement.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ReplaceRetired }));
        });
    }

    [Test]
    public void SplitRetirementsReturnEveryCandidateWithoutSelectingOne()
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeHistoricalCode(
                "agp",
                CreateOptions(false, true, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Split));
            Assert.That(result.Output, Is.Null);
            Assert.That(result.SourceForm, Is.EqualTo(LanguageCodeForm.IanaLanguage));
            Assert.That(result.TargetForm, Is.EqualTo(LanguageCodeForm.IanaLanguage));
            Assert.That(result.Entry?.Status, Is.EqualTo(LanguageCodeStatus.Retired));
            Assert.That(result.Entry?.Retirement?.RetiredCode, Is.EqualTo("agp"));
            Assert.That(result.ReplacementCandidates, Is.EqualTo(new[] { "apf", "prf" }));
            Assert.That(result.AppliedSteps, Is.Empty);
        });
    }

    [TestCase("aay")]
    [TestCase("pbz")]
    public void DeletedCodesWithoutReplacementAreReportedExplicitly(string code)
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeHistoricalCode(
                code,
                CreateOptions(false, true, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(
                LanguageNormalizationStatus.NoReplacement));
            Assert.That(result.Output, Is.Null);
            Assert.That(result.Entry?.Retirement?.RetiredCode, Is.EqualTo(code));
            Assert.That(result.ReplacementCandidates, Is.Empty);
            Assert.That(result.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void DeprecatedCodeWithoutPreferredValueIsNotFabricated()
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeHistoricalCode(
                "aoh",
                CreateOptions(true, false, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(
                LanguageNormalizationStatus.NoReplacement));
            Assert.That(result.Output, Is.Null);
            Assert.That(result.SourceForm, Is.EqualTo(LanguageCodeForm.IanaLanguage));
            Assert.That(result.ReplacementCandidates, Is.Empty);
            Assert.That(result.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void MultiHopRetirementChainsResolveOnceAndCyclesRemainFinite()
    {
        LanguageNormalizationOptions options = CreateOptions(false, true, false, true);
        LanguageNormalizationResult lexical =
            LanguageNormalizationEngine.NormalizeCodeLexically("fri", options);
        RetirementIndexData multiHopData = RetirementIndexBuilder.BuildIndex(
            new[]
            {
                CreateRetirement("fri", new[] { "aaa" }),
                CreateRetirement("aaa", new[] { "bbb" }),
                CreateRetirement("bbb", new[] { "ccc" })
            });
        RetirementIndexData cycleData = RetirementIndexBuilder.BuildIndex(
            new[]
            {
                CreateRetirement("fri", new[] { "aaa" }),
                CreateRetirement("aaa", new[] { "fri" })
            });

        LanguageNormalizationResult multiHop =
            LanguageNormalizationEngine.NormalizeHistoricalCodeResult(
                lexical,
                options,
                multiHopData);
        LanguageNormalizationResult cycle =
            LanguageNormalizationEngine.NormalizeHistoricalCodeResult(
                lexical,
                options,
                cycleData);

        Assert.Multiple(() =>
        {
            Assert.That(multiHop.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(multiHop.Output, Is.EqualTo("ccc"));
            Assert.That(multiHop.TargetForm, Is.EqualTo(LanguageCodeForm.Set3));
            Assert.That(multiHop.Entry?.Set3Code, Is.EqualTo("ccc"));
            Assert.That(multiHop.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ReplaceRetired }));
            Assert.That(cycle.Status, Is.EqualTo(
                LanguageNormalizationStatus.ReplacementCycle));
            Assert.That(cycle.Output, Is.Null);
            Assert.That(cycle.ReplacementCandidates, Is.Empty);
            Assert.That(cycle.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void InvalidReplacementEndpointDoesNotBecomeOutput()
    {
        LanguageNormalizationOptions options = CreateOptions(false, true, false, true);
        LanguageNormalizationResult lexical =
            LanguageNormalizationEngine.NormalizeCodeLexically("fri", options);
        RetirementIndexData data = RetirementIndexBuilder.BuildIndex(
            new[] { CreateRetirement("fri", new[] { "zzz" }) });

        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeHistoricalCodeResult(
                lexical,
                options,
                data);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.NotFound));
            Assert.That(result.Output, Is.Null);
            Assert.That(result.Entry?.Retirement?.RetiredCode, Is.EqualTo("fri"));
            Assert.That(result.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void LexicalChangesPrecedeReplacementAndSecondPassIsStable()
    {
        LanguageNormalizationOptions options = CreateOptions(false, true, true, true);
        LanguageNormalizationResult first =
            LanguageNormalizationEngine.NormalizeHistoricalCode("  FRI  ", options);
        LanguageNormalizationResult second =
            LanguageNormalizationEngine.NormalizeHistoricalCode(first.Output, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(first.Output, Is.EqualTo("fry"));
            Assert.That(first.AppliedSteps, Is.EqualTo(new[]
            {
                LanguageNormalizationStep.TrimWhitespace,
                LanguageNormalizationStep.NormalizeCase,
                LanguageNormalizationStep.ReplaceRetired
            }));
            Assert.That(second.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(second.Output, Is.EqualTo("fry"));
            Assert.That(second.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void LexicalFailuresPropagateWithoutHistoricalGuessing()
    {
        LanguageNormalizationOptions options = CreateOptions(true, true, false, true);
        LanguageNormalizationResult malformed =
            LanguageNormalizationEngine.NormalizeHistoricalCode("f1i", options);
        LanguageNormalizationResult missing =
            LanguageNormalizationEngine.NormalizeHistoricalCode("zzz", options);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Status, Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(malformed.Output, Is.Null);
            Assert.That(missing.Status, Is.EqualTo(LanguageNormalizationStatus.NotFound));
            Assert.That(missing.Output, Is.Null);
            Assert.That(malformed.AppliedSteps, Is.Empty);
            Assert.That(missing.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void HistoricalResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageNormalizationOptions splitOptions = CreateOptions(false, true, false, true);
        LanguageNormalizationOptions deprecatedOptions = CreateOptions(true, false, false, true);
        LanguageNormalizationResult firstSplit =
            LanguageNormalizationEngine.NormalizeHistoricalCode("agp", splitOptions);
        LanguageNormalizationResult secondSplit =
            LanguageNormalizationEngine.NormalizeHistoricalCode("agp", splitOptions);
        LanguageNormalizationResult firstDeprecated =
            LanguageNormalizationEngine.NormalizeHistoricalCode("iw", deprecatedOptions);
        LanguageNormalizationResult secondDeprecated =
            LanguageNormalizationEngine.NormalizeHistoricalCode("iw", deprecatedOptions);

        firstSplit.ReplacementCandidates[0] = "changed";
        firstSplit.Entry!.Retirement!.ReplacementCodes[0] = "changed";
        firstDeprecated.AppliedSteps[0] = LanguageNormalizationStep.NormalizeCase;
        firstDeprecated.Entry!.Set3Code = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(secondSplit.ReplacementCandidates, Is.EqualTo(new[] { "apf", "prf" }));
            Assert.That(secondSplit.Entry?.Retirement?.ReplacementCodes, Is.EqualTo(
                new[] { "apf", "prf" }));
            Assert.That(secondDeprecated.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.ReplaceDeprecated }));
            Assert.That(secondDeprecated.Entry?.Set3Code, Is.EqualTo("heb"));
            Assert.That(
                LanguageNormalizationEngine.NormalizeHistoricalCode("iw", deprecatedOptions)
                    .Output,
                Is.EqualTo("he"));
        });
    }

    private static LanguageNormalizationOptions CreateOptions(
        bool replaceDeprecated,
        bool replaceRetired,
        bool trimWhitespace,
        bool normalizeCase)
    {
        return new LanguageNormalizationOptions
        {
            ReplaceDeprecated = replaceDeprecated,
            ReplaceRetired = replaceRetired,
            TrimWhitespace = trimWhitespace,
            NormalizeCase = normalizeCase
        };
    }

    private static RetirementRow CreateRetirement(string code, string[] replacements)
    {
        return new RetirementRow(
            code,
            "Former " + code,
            replacements.Length > 1
                ? LanguageRetirementReason.Split
                : LanguageRetirementReason.Change,
            replacements,
            null,
            "2000-01-01");
    }
}
