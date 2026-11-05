using languagecodes;

namespace languagecodes.Tests;

public class ReconciledLanguageInvariantsTests
{
    [Test]
    public void CompleteReconciledSchemaHasNoViolations()
    {
        ReconciledLanguageEntity macro = CreateEntity("ara", "Arabic", ReconciledLanguageScope.Macrolanguage);
        macro.Alpha2Code = "ar";
        macro.Set2TerminologyCode = "ara";
        macro.Set2BibliographicCode = "ara";
        macro.InSet1 = true;
        macro.InSet2 = true;
        macro.Aliases = new[]
        {
            new ReconciledLanguageAlias
            {
                Value = "ar",
                Kind = ReconciledAliasKind.Alpha2,
                PreferredValue = "ara",
                Source = ReconciledSource.Iso639Set1
            }
        };

        ReconciledLanguageEntity member = CreateEntity("aao", "Algerian Saharan Arabic", ReconciledLanguageScope.Individual);
        ReconciledLanguageEntity groupParent = CreateSet5Entity("afa", "Afro-Asiatic languages", ReconciledGroupKind.Family);
        ReconciledLanguageEntity groupChild = CreateSet5Entity("sem", "Semitic languages", ReconciledGroupKind.LanguageGroup);
        ReconciledLanguageEntity replacement = CreateEntity("fry", "Western Frisian", ReconciledLanguageScope.Individual);
        ReconciledLanguageEntity retired = CreateEntity("fri", "Western Frisian", ReconciledLanguageScope.Individual);
        retired.Status = ReconciledEntityStatus.Retired;
        retired.Retirement = new ReconciledLanguageRetirement
        {
            RetiredCode = "fri",
            FormerReferenceName = "Western Frisian",
            Reason = ReconciledRetirementReason.Change,
            ReplacementCodes = new[] { "fry" },
            EffectiveDate = new DateOnly(2005, 11, 16)
        };

        ReconciledLanguageModel model = new ReconciledLanguageModel
        {
            Entities = new[] { macro, member, groupParent, groupChild, replacement, retired },
            ReservedRanges = new[]
            {
                new ReconciledReservedRange
                {
                    FirstCode = "qaa",
                    LastCode = "qtz",
                    Source = ReconciledSource.Iso639Set2,
                    Purpose = "Local use"
                }
            },
            Relationships = new[]
            {
                new ReconciledRelationship
                {
                    ParentKey = "ara",
                    ChildKey = "aao",
                    Kind = ReconciledRelationshipKind.MacrolanguageMember,
                    Status = ReconciledRelationshipStatus.Active,
                    Source = ReconciledSource.Iso639Set3
                },
                new ReconciledRelationship
                {
                    ParentKey = "afa",
                    ChildKey = "sem",
                    Kind = ReconciledRelationshipKind.Set5ParentChild,
                    Status = ReconciledRelationshipStatus.Active,
                    Source = ReconciledSource.Iso639Set5
                }
            },
            IanaLinks = new[]
            {
                new ReconciledIanaLink
                {
                    EntityKey = "ara",
                    RecordType = ReconciledIanaRecordType.Language,
                    Value = "ar",
                    Descriptions = new[] { "Arabic" },
                    Prefixes = Array.Empty<string>(),
                    SuppressScript = "Arab",
                    Scope = "macrolanguage",
                    AddedDate = "2005-10-16"
                }
            }
        };

        Assert.That(ReconciledLanguageInvariants.FindViolations(model), Is.Empty);
    }

    [Test]
    public void SetMembershipMustAgreeWithEquivalentCodeForms()
    {
        ReconciledLanguageEntity entity = CreateEntity("eng", "English", ReconciledLanguageScope.Individual);
        entity.Alpha2Code = "en";
        entity.InSet1 = false;
        entity.InSet2 = true;
        entity.Set2TerminologyCode = "eng";

        string[] violations = ValidateSingle(entity);

        Assert.That(violations, Does.Contain("Entity 'eng' has a Set 1 code but does not belong to Set 1."));
        Assert.That(violations, Does.Contain("Entity 'eng' belongs to Set 2 but does not have both terminology and bibliographic code forms."));
    }

    [Test]
    public void NamesAliasesAndStatusesRetainTheirRequiredMeaning()
    {
        ReconciledLanguageEntity entity = CreateEntity("eng", "English", ReconciledLanguageScope.Individual);
        entity.Status = ReconciledEntityStatus.Retired;
        entity.Names = new[]
        {
            new ReconciledLanguageName
            {
                Text = "English",
                NameLanguage = "en",
                Kind = ReconciledNameKind.English,
                Source = ReconciledSource.Iso639Set2
            }
        };
        entity.Aliases = new[]
        {
            new ReconciledLanguageAlias
            {
                Value = "enm",
                PreferredValue = "outside",
                Kind = ReconciledAliasKind.IanaDeprecated,
                Source = ReconciledSource.Iana
            }
        };

        string[] violations = ValidateSingle(entity);

        Assert.That(violations, Does.Contain("Entity 'eng' has no reference name."));
        Assert.That(violations, Does.Contain("Alias 'enm' on entity 'eng' prefers a code outside that entity."));
        Assert.That(violations, Does.Contain("Retired entity 'eng' has no retirement metadata."));
    }

    [Test]
    public void SpecialAndReservedRepresentationsAreExplicit()
    {
        ReconciledLanguageEntity special = CreateEntity("und", "Undetermined", ReconciledLanguageScope.Special);
        special.LanguageType = ReconciledLanguageType.Unknown;
        ReconciledLanguageModel model = new ReconciledLanguageModel
        {
            Entities = new[] { special },
            ReservedRanges = new[]
            {
                new ReconciledReservedRange
                {
                    FirstCode = "qtz",
                    LastCode = "qaa",
                    Source = ReconciledSource.Unknown,
                    Purpose = ""
                }
            }
        };

        string[] violations = ReconciledLanguageInvariants.FindViolations(model);

        Assert.That(violations, Does.Contain("Special entity 'und' does not have the special language type."));
        Assert.That(violations, Does.Contain("Reserved range 'qtz' through 'qaa' is reversed."));
        Assert.That(violations, Does.Contain("A reserved range has no Set 2 or IANA source."));
        Assert.That(violations, Does.Contain("A reserved range has no documented purpose."));
    }

    [Test]
    public void RelationshipsAndIanaLinksRequireReconciledEndpoints()
    {
        ReconciledLanguageEntity individual = CreateEntity("eng", "English", ReconciledLanguageScope.Individual);
        ReconciledLanguageEntity other = CreateEntity("enm", "Middle English", ReconciledLanguageScope.Individual);
        ReconciledLanguageModel model = new ReconciledLanguageModel
        {
            Entities = new[] { individual, other },
            Relationships = new[]
            {
                new ReconciledRelationship
                {
                    ParentKey = "eng",
                    ChildKey = "enm",
                    Kind = ReconciledRelationshipKind.MacrolanguageMember
                },
                new ReconciledRelationship
                {
                    ParentKey = "eng",
                    ChildKey = "missing",
                    Kind = ReconciledRelationshipKind.Set5ParentChild
                }
            },
            IanaLinks = new[]
            {
                new ReconciledIanaLink
                {
                    EntityKey = "missing",
                    Value = "zz",
                    RecordType = ReconciledIanaRecordType.Language,
                    Descriptions = Array.Empty<string>()
                }
            }
        };

        string[] violations = ReconciledLanguageInvariants.FindViolations(model);

        Assert.That(violations, Does.Contain("Macrolanguage relationship 'eng' to 'enm' has incompatible endpoint scopes."));
        Assert.That(violations, Does.Contain("Relationship 'eng' to 'missing' has a dangling endpoint."));
        Assert.That(violations, Does.Contain("IANA value 'zz' links to unknown entity 'missing'."));
        Assert.That(violations, Does.Contain("IANA value 'zz' has no descriptions."));
    }

    [Test]
    public void RetirementRulesPreserveSingularAndSplitReplacementSemantics()
    {
        ReconciledLanguageEntity firstReplacement = CreateEntity("aaa", "First", ReconciledLanguageScope.Individual);
        ReconciledLanguageEntity secondReplacement = CreateEntity("aab", "Second", ReconciledLanguageScope.Individual);
        ReconciledLanguageEntity split = CreateEntity("aac", "Former", ReconciledLanguageScope.Individual);
        split.Status = ReconciledEntityStatus.Retired;
        split.Retirement = new ReconciledLanguageRetirement
        {
            RetiredCode = "aac",
            FormerReferenceName = "Former",
            Reason = ReconciledRetirementReason.Split,
            ReplacementCodes = new[] { "aaa", "aab" },
            Remedy = "Split into aaa and aab.",
            EffectiveDate = new DateOnly(2020, 1, 1)
        };
        ReconciledLanguageModel validModel = new ReconciledLanguageModel
        {
            Entities = new[] { firstReplacement, secondReplacement, split }
        };

        Assert.That(ReconciledLanguageInvariants.FindViolations(validModel), Is.Empty);

        split.Retirement.Reason = ReconciledRetirementReason.Change;
        string[] violations = ReconciledLanguageInvariants.FindViolations(validModel);
        Assert.That(violations, Does.Contain("Retirement metadata for 'aac' requires exactly one replacement."));

        split.Retirement.ReplacementCodes = new[] { "aac" };
        violations = ReconciledLanguageInvariants.FindViolations(validModel);
        Assert.That(violations, Does.Contain("Retirement metadata for 'aac' replaces the entity with itself."));
    }

    private static ReconciledLanguageEntity CreateEntity(
        string code,
        string name,
        ReconciledLanguageScope scope)
    {
        return new ReconciledLanguageEntity
        {
            Key = code,
            Set3Code = code,
            InSet3 = true,
            Scope = scope,
            LanguageType = ReconciledLanguageType.Living,
            Names = new[]
            {
                new ReconciledLanguageName
                {
                    Text = name,
                    NameLanguage = "en",
                    Kind = ReconciledNameKind.Reference,
                    Source = ReconciledSource.Iso639Set3,
                    IsReferenceName = true
                }
            }
        };
    }

    private static ReconciledLanguageEntity CreateSet5Entity(
        string code,
        string name,
        ReconciledGroupKind groupKind)
    {
        ReconciledLanguageEntity entity = CreateEntity(code, name, ReconciledLanguageScope.Collection);
        entity.Set3Code = null;
        entity.InSet3 = false;
        entity.Set2TerminologyCode = code;
        entity.Set2BibliographicCode = code;
        entity.Set5Code = code;
        entity.InSet2 = true;
        entity.InSet5 = true;
        entity.LanguageType = ReconciledLanguageType.Unknown;
        entity.GroupKind = groupKind;
        return entity;
    }

    private static string[] ValidateSingle(ReconciledLanguageEntity entity)
    {
        ReconciledLanguageModel model = new ReconciledLanguageModel
        {
            Entities = new[] { entity }
        };
        return ReconciledLanguageInvariants.FindViolations(model);
    }
}
