using languagecodes;

namespace languagecodes.Tests;

public class LanguageRetirementEngineTests
{
    [Test]
    public void RetirementLookupReturnsCompleteAuthoritativeRecord()
    {
        LanguageRetirement? retirement = LanguageRetirementEngine.GetRetirement("agp");

        Assert.Multiple(() =>
        {
            Assert.That(retirement, Is.Not.Null);
            Assert.That(retirement?.RetiredCode, Is.EqualTo("agp"));
            Assert.That(retirement?.FormerReferenceName, Is.EqualTo("Paranan"));
            Assert.That(retirement?.Reason, Is.EqualTo(LanguageRetirementReason.Split));
            Assert.That(retirement?.ReplacementCodes, Is.EqualTo(new[] { "apf", "prf" }));
            Assert.That(
                retirement?.Remedy,
                Is.EqualTo(
                    "Split into Pahanan Agta [apf] and Paranan [prf] (new identifier)"));
            Assert.That(retirement?.EffectiveDate, Is.EqualTo("2010-01-18"));
        });
    }

    [Test]
    public void RetirementLookupIsExactAndReturnsNullForActiveOrUnknownCodes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LanguageRetirementEngine.GetRetirement("eng"), Is.Null);
            Assert.That(LanguageRetirementEngine.GetRetirement("AGP"), Is.Null);
            Assert.That(LanguageRetirementEngine.GetRetirement(string.Empty), Is.Null);
            Assert.That(LanguageRetirementEngine.GetRetirement(null), Is.Null);
            Assert.That(LanguageRetirementEngine.ResolveReplacementChain("eng"), Is.Null);
            Assert.That(LanguageRetirementEngine.ResolveReplacementChain(null), Is.Null);
        });
    }

    [Test]
    public void GeneratedChainsPreserveSingularSplitAndNoReplacementOutcomes()
    {
        RetirementChainRow? singular =
            LanguageRetirementEngine.ResolveReplacementChain("fri");
        RetirementChainRow? split =
            LanguageRetirementEngine.ResolveReplacementChain("agp");
        RetirementChainRow? deleted =
            LanguageRetirementEngine.ResolveReplacementChain("aay");

        Assert.Multiple(() =>
        {
            Assert.That(singular?.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(singular?.Codes, Is.EqualTo(new[] { "fri", "fry" }));
            Assert.That(singular?.FinalCode, Is.EqualTo("fry"));
            Assert.That(singular?.ReplacementCandidates, Is.Empty);
            Assert.That(singular?.Retirements.Select(row => row.RetiredCode), Is.EqualTo(
                new[] { "fri" }));
            Assert.That(split?.Status, Is.EqualTo(LanguageNormalizationStatus.Split));
            Assert.That(split?.Codes, Is.EqualTo(new[] { "agp" }));
            Assert.That(split?.FinalCode, Is.Null);
            Assert.That(split?.ReplacementCandidates, Is.EqualTo(new[] { "apf", "prf" }));
            Assert.That(split?.Retirements, Has.Length.EqualTo(1));
            Assert.That(
                split?.Retirements[0].Remedy,
                Is.EqualTo(
                    "Split into Pahanan Agta [apf] and Paranan [prf] (new identifier)"));
            Assert.That(deleted?.Status, Is.EqualTo(LanguageNormalizationStatus.NoReplacement));
            Assert.That(deleted?.Codes, Is.EqualTo(new[] { "aay" }));
            Assert.That(deleted?.FinalCode, Is.Null);
            Assert.That(deleted?.ReplacementCandidates, Is.Empty);
            Assert.That(deleted?.Retirements[0].Reason, Is.EqualTo(
                LanguageRetirementReason.Nonexistent));
        });
    }

    [Test]
    public void MultiHopResolutionRetainsEveryOneToOneRetirementRecord()
    {
        RetirementIndexData data = RetirementIndexBuilder.BuildIndex(
            new[]
            {
                CreateRetirement("aaa", new[] { "bbb" }, "Use bbb."),
                CreateRetirement("bbb", new[] { "ccc" }, "Use ccc.")
            });

        RetirementChainRow? chain =
            LanguageRetirementEngine.ResolveReplacementChainEntry("aaa", data);

        Assert.Multiple(() =>
        {
            Assert.That(chain?.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(chain?.Codes, Is.EqualTo(new[] { "aaa", "bbb", "ccc" }));
            Assert.That(chain?.FinalCode, Is.EqualTo("ccc"));
            Assert.That(chain?.ReplacementCandidates, Is.Empty);
            Assert.That(
                chain?.Retirements.Select(row => row.RetiredCode),
                Is.EqualTo(new[] { "aaa", "bbb" }));
            Assert.That(
                chain?.Retirements.Select(row => row.Remedy),
                Is.EqualTo(new[] { "Use bbb.", "Use ccc." }));
        });
    }

    [Test]
    public void SplitCandidatesRemainAuthoritativeAndAreNeverSelected()
    {
        RetirementIndexData data = RetirementIndexBuilder.BuildIndex(
            new[]
            {
                CreateRetirement(
                    "aaa",
                    new[] { "zzz", "bbb", "ccc" },
                    "Authoritative split remedy.")
            });

        RetirementChainRow? chain =
            LanguageRetirementEngine.ResolveReplacementChainEntry("aaa", data);

        Assert.Multiple(() =>
        {
            Assert.That(chain?.Status, Is.EqualTo(LanguageNormalizationStatus.Split));
            Assert.That(chain?.FinalCode, Is.Null);
            Assert.That(chain?.ReplacementCandidates, Is.EqualTo(
                new[] { "zzz", "bbb", "ccc" }));
            Assert.That(chain?.Retirements[0].Remedy, Is.EqualTo(
                "Authoritative split remedy."));
        });
    }

    [Test]
    public void ReplacementCyclesRemainFiniteAndRetainVisitedRecords()
    {
        RetirementIndexData data = RetirementIndexBuilder.BuildIndex(
            new[]
            {
                CreateRetirement("aaa", new[] { "bbb" }, "First hop."),
                CreateRetirement("bbb", new[] { "aaa" }, "Second hop.")
            });

        RetirementChainRow? chain =
            LanguageRetirementEngine.ResolveReplacementChainEntry("aaa", data);

        Assert.Multiple(() =>
        {
            Assert.That(chain?.Status, Is.EqualTo(
                LanguageNormalizationStatus.ReplacementCycle));
            Assert.That(chain?.Codes, Is.EqualTo(new[] { "aaa", "bbb" }));
            Assert.That(chain?.FinalCode, Is.Null);
            Assert.That(chain?.ReplacementCandidates, Is.Empty);
            Assert.That(
                chain?.Retirements.Select(row => row.RetiredCode),
                Is.EqualTo(new[] { "aaa", "bbb" }));
        });
    }

    [Test]
    public void RetirementAndChainResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageRetirement? firstRetirement =
            LanguageRetirementEngine.GetRetirement("agp");
        LanguageRetirement? secondRetirement =
            LanguageRetirementEngine.GetRetirement("agp");
        RetirementChainRow? firstChain =
            LanguageRetirementEngine.ResolveReplacementChain("agp");
        RetirementChainRow? secondChain =
            LanguageRetirementEngine.ResolveReplacementChain("agp");
        string? originalRemedy = secondChain!.Retirements[0].Remedy;

        firstRetirement!.ReplacementCodes[0] = "changed";
        firstRetirement.Remedy = "changed";
        firstChain!.Codes[0] = "changed";
        firstChain.ReplacementCandidates[0] = "changed";
        firstChain.Retirements[0].ReplacementCodes[0] = "changed";
        firstChain.Retirements[0].Remedy = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(firstRetirement, Is.Not.SameAs(secondRetirement));
            Assert.That(secondRetirement?.ReplacementCodes[0], Is.EqualTo("apf"));
            Assert.That(secondRetirement?.Remedy, Is.Not.EqualTo("changed"));
            Assert.That(firstChain, Is.Not.SameAs(secondChain));
            Assert.That(secondChain?.Codes[0], Is.EqualTo("agp"));
            Assert.That(secondChain?.ReplacementCandidates[0], Is.EqualTo("apf"));
            Assert.That(secondChain?.Retirements[0].ReplacementCodes[0], Is.EqualTo("apf"));
            Assert.That(secondChain?.Retirements[0].Remedy, Is.EqualTo(originalRemedy));
            Assert.That(
                LanguageRetirementEngine.ResolveReplacementChain("agp")
                    ?.ReplacementCandidates[0],
                Is.EqualTo("apf"));
        });
    }

    private static RetirementRow CreateRetirement(
        string code,
        string[] replacements,
        string remedy)
    {
        return new RetirementRow(
            code,
            "Former " + code,
            replacements.Length > 1
                ? LanguageRetirementReason.Split
                : LanguageRetirementReason.Change,
            replacements,
            remedy,
            "2000-01-01");
    }
}
