using languagecodes;

namespace languagecodes.Tests;

public class LanguageMacrolanguageNormalizationEngineTests
{
    [Test]
    public void PreservePolicyLeavesIndividualsAndTheirCodeFormsUnchanged()
    {
        LanguageNormalizationResult canonical =
            LanguageNormalizationEngine.NormalizeMacrolanguageCode(
                "aao",
                CreateOptions(MacrolanguageNormalizationPolicy.Preserve, false, true));
        LanguageNormalizationResult caseNormalized =
            LanguageNormalizationEngine.NormalizeMacrolanguageCode(
                "AAO",
                CreateOptions(MacrolanguageNormalizationPolicy.Preserve, false, true));

        Assert.Multiple(() =>
        {
            Assert.That(canonical.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(canonical.Output, Is.EqualTo("aao"));
            Assert.That(canonical.Entry?.Set3Code, Is.EqualTo("aao"));
            Assert.That(canonical.TargetForm, Is.EqualTo(LanguageCodeForm.Set3));
            Assert.That(canonical.MacrolanguageRelationship, Is.Null);
            Assert.That(canonical.AppliedSteps, Is.Empty);
            Assert.That(caseNormalized.Status, Is.EqualTo(
                LanguageNormalizationStatus.Success));
            Assert.That(caseNormalized.Output, Is.EqualTo("aao"));
            Assert.That(caseNormalized.Entry?.Set3Code, Is.EqualTo("aao"));
            Assert.That(caseNormalized.MacrolanguageRelationship, Is.Null);
            Assert.That(caseNormalized.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.NormalizeCase }));
        });
    }

    [TestCase("aao", "ara", "aao", LanguageCodeForm.Set3)]
    [TestCase("cmn", "zho", "cmn", LanguageCodeForm.Set3)]
    [TestCase("nb", "nor", "nob", LanguageCodeForm.Alpha2)]
    [TestCase("ajp", "ara", "apc", LanguageCodeForm.IanaLanguage)]
    public void FoldPolicyUsesTheResolvedMembersActiveRelationship(
        string input,
        string expectedParent,
        string expectedChild,
        LanguageCodeForm expectedSourceForm)
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeMacrolanguageCode(
                input,
                CreateOptions(
                    MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
                    false,
                    true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(result.OriginalInput, Is.EqualTo(input));
            Assert.That(result.Output, Is.EqualTo(expectedParent));
            Assert.That(result.SourceForm, Is.EqualTo(expectedSourceForm));
            Assert.That(result.TargetForm, Is.EqualTo(LanguageCodeForm.Set3));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo(expectedParent));
            Assert.That(result.Entry?.Scope, Is.EqualTo(LanguageScope.Macrolanguage));
            Assert.That(result.MacrolanguageRelationship?.ParentCode, Is.EqualTo(
                expectedParent));
            Assert.That(result.MacrolanguageRelationship?.ChildCode, Is.EqualTo(
                expectedChild));
            Assert.That(result.MacrolanguageRelationship?.Kind, Is.EqualTo(
                LanguageRelationshipKind.MacrolanguageMember));
            Assert.That(result.MacrolanguageRelationship?.SourceSet, Is.EqualTo(
                Iso639Set.Set3));
            Assert.That(result.MacrolanguageRelationship?.IsRetired, Is.False);
            Assert.That(result.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.FoldMacrolanguage }));
        });
    }

    [TestCase("ara")]
    [TestCase("eng")]
    [TestCase("afa")]
    [TestCase("qaa")]
    [TestCase("fri")]
    public void EntitiesWithoutAnActiveParentRemainUnchanged(string code)
    {
        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeMacrolanguageCode(
                code,
                CreateOptions(
                    MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
                    false,
                    true));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(result.Output, Is.EqualTo(code));
            Assert.That(result.MacrolanguageRelationship, Is.Null);
            Assert.That(result.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void RetiredRelationshipsAreNeverUsedForNormalization()
    {
        LanguageRow[] languages =
        {
            CreateLanguage("set3:aaa", "aaa", LanguageScope.Macrolanguage),
            CreateLanguage("set3:bbb", "bbb", LanguageScope.Individual)
        };
        RelationshipRow[] rows =
        {
            CreateRelationship(
                "aaa",
                "bbb",
                ReconciledRelationshipStatus.Retired)
        };
        RelationshipIndexData relationships = RelationshipIndexBuilder.BuildIndex(rows);
        LanguageCatalogData catalog = LanguageCatalogBuilder.BuildCatalog(
            languages,
            Array.Empty<NameRow>(),
            Array.Empty<AliasRow>(),
            rows,
            Array.Empty<RetirementRow>());
        LanguageNormalizationResult normalized = CreateNormalizedMember(
            "bbb",
            catalog.EntriesByEntityIndex[1]);

        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeMacrolanguageCodeResult(
                normalized,
                CreateOptions(
                    MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
                    false,
                    true),
                relationships,
                catalog);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(result.Output, Is.EqualTo("bbb"));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo("bbb"));
            Assert.That(result.MacrolanguageRelationship, Is.Null);
            Assert.That(result.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void SelectedParentAndReportedRelationshipRemainDeterministic()
    {
        LanguageRow[] languages =
        {
            CreateLanguage("set3:aaa", "aaa", LanguageScope.Macrolanguage),
            CreateLanguage("set3:bbb", "bbb", LanguageScope.Macrolanguage),
            CreateLanguage("set3:ccc", "ccc", LanguageScope.Individual)
        };
        RelationshipRow[] rows =
        {
            CreateRelationship("bbb", "ccc", ReconciledRelationshipStatus.Active),
            CreateRelationship("aaa", "ccc", ReconciledRelationshipStatus.Active)
        };
        RelationshipIndexData relationships = RelationshipIndexBuilder.BuildIndex(rows);
        LanguageCatalogData catalog = LanguageCatalogBuilder.BuildCatalog(
            languages,
            Array.Empty<NameRow>(),
            Array.Empty<AliasRow>(),
            rows,
            Array.Empty<RetirementRow>());
        LanguageNormalizationResult normalized = CreateNormalizedMember(
            "ccc",
            catalog.EntriesByEntityIndex[2]);

        LanguageNormalizationResult result =
            LanguageNormalizationEngine.NormalizeMacrolanguageCodeResult(
                normalized,
                CreateOptions(
                    MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
                    false,
                    true),
                relationships,
                catalog);

        Assert.Multiple(() =>
        {
            Assert.That(result.Output, Is.EqualTo("aaa"));
            Assert.That(result.Entry?.Set3Code, Is.EqualTo("aaa"));
            Assert.That(result.MacrolanguageRelationship?.ParentCode, Is.EqualTo("aaa"));
            Assert.That(result.MacrolanguageRelationship?.ChildCode, Is.EqualTo("ccc"));
            Assert.That(result.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.FoldMacrolanguage }));
        });
    }

    [Test]
    public void LexicalChangesPrecedeFoldingAndSecondPassIsStable()
    {
        LanguageNormalizationOptions options = CreateOptions(
            MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
            true,
            true);
        LanguageNormalizationResult first =
            LanguageNormalizationEngine.NormalizeMacrolanguageCode("  AAO  ", options);
        LanguageNormalizationResult second =
            LanguageNormalizationEngine.NormalizeMacrolanguageCode(first.Output, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(LanguageNormalizationStatus.Success));
            Assert.That(first.Output, Is.EqualTo("ara"));
            Assert.That(first.AppliedSteps, Is.EqualTo(new[]
            {
                LanguageNormalizationStep.TrimWhitespace,
                LanguageNormalizationStep.NormalizeCase,
                LanguageNormalizationStep.FoldMacrolanguage
            }));
            Assert.That(second.Status, Is.EqualTo(
                LanguageNormalizationStatus.AlreadyCanonical));
            Assert.That(second.Output, Is.EqualTo("ara"));
            Assert.That(second.MacrolanguageRelationship, Is.Null);
            Assert.That(second.AppliedSteps, Is.Empty);
        });
    }

    [Test]
    public void LexicalFailuresPropagateWithoutRelationshipGuessing()
    {
        LanguageNormalizationOptions options = CreateOptions(
            MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
            false,
            true);
        LanguageNormalizationResult malformed =
            LanguageNormalizationEngine.NormalizeMacrolanguageCode("a1o", options);
        LanguageNormalizationResult missing =
            LanguageNormalizationEngine.NormalizeMacrolanguageCode("zzz", options);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Status, Is.EqualTo(LanguageNormalizationStatus.Invalid));
            Assert.That(malformed.Output, Is.Null);
            Assert.That(missing.Status, Is.EqualTo(LanguageNormalizationStatus.NotFound));
            Assert.That(missing.Output, Is.Null);
            Assert.That(malformed.MacrolanguageRelationship, Is.Null);
            Assert.That(missing.MacrolanguageRelationship, Is.Null);
        });
    }

    [Test]
    public void FoldResultsAreRecursiveDefensiveSnapshots()
    {
        LanguageNormalizationOptions options = CreateOptions(
            MacrolanguageNormalizationPolicy.FoldToMacrolanguage,
            false,
            true);
        LanguageNormalizationResult first =
            LanguageNormalizationEngine.NormalizeMacrolanguageCode("aao", options);
        LanguageNormalizationResult second =
            LanguageNormalizationEngine.NormalizeMacrolanguageCode("aao", options);
        string originalName = second.Entry!.Names[0].Text!;

        first.Entry!.Set3Code = "changed";
        first.Entry.Names[0].Text = "changed";
        first.MacrolanguageRelationship!.ParentCode = "changed";
        first.AppliedSteps[0] = LanguageNormalizationStep.NormalizeCase;

        Assert.Multiple(() =>
        {
            Assert.That(second.Entry?.Set3Code, Is.EqualTo("ara"));
            Assert.That(second.Entry?.Names[0].Text, Is.EqualTo(originalName));
            Assert.That(second.MacrolanguageRelationship?.ParentCode, Is.EqualTo("ara"));
            Assert.That(second.MacrolanguageRelationship?.ChildCode, Is.EqualTo("aao"));
            Assert.That(second.AppliedSteps, Is.EqualTo(
                new[] { LanguageNormalizationStep.FoldMacrolanguage }));
            Assert.That(
                LanguageNormalizationEngine.NormalizeMacrolanguageCode("aao", options)
                    .Output,
                Is.EqualTo("ara"));
        });
    }

    private static LanguageNormalizationOptions CreateOptions(
        MacrolanguageNormalizationPolicy policy,
        bool trimWhitespace,
        bool normalizeCase)
    {
        return new LanguageNormalizationOptions
        {
            MacrolanguagePolicy = policy,
            TrimWhitespace = trimWhitespace,
            NormalizeCase = normalizeCase
        };
    }

    private static LanguageRow CreateLanguage(
        string key,
        string code,
        LanguageScope scope)
    {
        return new LanguageRow(
            key,
            null,
            null,
            null,
            code,
            null,
            scope,
            LanguageType.Living,
            LanguageCodeStatus.Active,
            ReconciledGroupKind.None,
            null);
    }

    private static RelationshipRow CreateRelationship(
        string parentCode,
        string childCode,
        ReconciledRelationshipStatus status)
    {
        return new RelationshipRow(
            parentCode,
            childCode,
            LanguageRelationshipKind.MacrolanguageMember,
            status,
            ReconciledSource.Iso639Set3);
    }

    private static LanguageNormalizationResult CreateNormalizedMember(
        string code,
        LanguageCodeEntry entry)
    {
        return new LanguageNormalizationResult
        {
            Status = LanguageNormalizationStatus.AlreadyCanonical,
            OriginalInput = code,
            Output = code,
            SourceForm = LanguageCodeForm.Set3,
            TargetForm = LanguageCodeForm.Set3,
            Entry = entry
        };
    }
}
