using languagecodes;

namespace languagecodes.Tests;

public class PublicEnumTests
{
    [Test]
    public void IsoAndCatalogEnumValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)Iso639Set.None, Is.EqualTo(0));
            Assert.That((int)Iso639Set.Set1, Is.EqualTo(1));
            Assert.That((int)Iso639Set.Set2, Is.EqualTo(2));
            Assert.That((int)Iso639Set.Set3, Is.EqualTo(3));
            Assert.That((int)Iso639Set.Set5, Is.EqualTo(5));

            Assert.That((int)LanguageCodeForm.Unknown, Is.EqualTo(0));
            Assert.That((int)LanguageCodeForm.Alpha2, Is.EqualTo(1));
            Assert.That((int)LanguageCodeForm.Set2Terminology, Is.EqualTo(2));
            Assert.That((int)LanguageCodeForm.Set2Bibliographic, Is.EqualTo(3));
            Assert.That((int)LanguageCodeForm.Set3, Is.EqualTo(4));
            Assert.That((int)LanguageCodeForm.Set5, Is.EqualTo(5));
            Assert.That((int)LanguageCodeForm.RetiredSet3, Is.EqualTo(6));
            Assert.That((int)LanguageCodeForm.IanaLanguage, Is.EqualTo(7));
            Assert.That((int)LanguageCodeForm.IanaExtlang, Is.EqualTo(8));
            Assert.That((int)LanguageCodeForm.GrandfatheredTag, Is.EqualTo(9));
            Assert.That((int)LanguageCodeForm.RedundantTag, Is.EqualTo(10));
            Assert.That((int)LanguageCodeForm.PrivateUse, Is.EqualTo(11));

            Assert.That((int)LanguageScope.Unknown, Is.EqualTo(0));
            Assert.That((int)LanguageScope.Individual, Is.EqualTo(1));
            Assert.That((int)LanguageScope.Macrolanguage, Is.EqualTo(2));
            Assert.That((int)LanguageScope.Collection, Is.EqualTo(3));
            Assert.That((int)LanguageScope.Special, Is.EqualTo(4));

            Assert.That((int)LanguageType.Unknown, Is.EqualTo(0));
            Assert.That((int)LanguageType.Living, Is.EqualTo(1));
            Assert.That((int)LanguageType.Extinct, Is.EqualTo(2));
            Assert.That((int)LanguageType.Ancient, Is.EqualTo(3));
            Assert.That((int)LanguageType.Historical, Is.EqualTo(4));
            Assert.That((int)LanguageType.Constructed, Is.EqualTo(5));
            Assert.That((int)LanguageType.Special, Is.EqualTo(6));

            Assert.That((int)LanguageCodeStatus.Unknown, Is.EqualTo(0));
            Assert.That((int)LanguageCodeStatus.Active, Is.EqualTo(1));
            Assert.That((int)LanguageCodeStatus.Retired, Is.EqualTo(2));
            Assert.That((int)LanguageCodeStatus.Deprecated, Is.EqualTo(3));
            Assert.That((int)LanguageCodeStatus.Reserved, Is.EqualTo(4));
            Assert.That((int)LanguageCodeStatus.PrivateUse, Is.EqualTo(5));
            Assert.That((int)LanguageCodeStatus.Unassigned, Is.EqualTo(6));
        });
    }

    [Test]
    public void DescriptiveEnumValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)LanguageNameKind.Reference, Is.EqualTo(0));
            Assert.That((int)LanguageNameKind.Print, Is.EqualTo(1));
            Assert.That((int)LanguageNameKind.Inverted, Is.EqualTo(2));
            Assert.That((int)LanguageNameKind.English, Is.EqualTo(3));
            Assert.That((int)LanguageNameKind.French, Is.EqualTo(4));
            Assert.That((int)LanguageNameKind.IanaDescription, Is.EqualTo(5));

            Assert.That((int)LanguageAliasKind.Alpha2, Is.EqualTo(0));
            Assert.That((int)LanguageAliasKind.Set2Terminology, Is.EqualTo(1));
            Assert.That((int)LanguageAliasKind.Set2Bibliographic, Is.EqualTo(2));
            Assert.That((int)LanguageAliasKind.Set3, Is.EqualTo(3));
            Assert.That((int)LanguageAliasKind.Set5, Is.EqualTo(4));
            Assert.That((int)LanguageAliasKind.RetiredSet3, Is.EqualTo(5));
            Assert.That((int)LanguageAliasKind.IanaDeprecated, Is.EqualTo(6));
            Assert.That((int)LanguageAliasKind.IanaPreferred, Is.EqualTo(7));
            Assert.That((int)LanguageAliasKind.Grandfathered, Is.EqualTo(8));
            Assert.That((int)LanguageAliasKind.Overlong, Is.EqualTo(9));

            Assert.That((int)LanguageRelationshipKind.MacrolanguageMember, Is.EqualTo(0));
            Assert.That((int)LanguageRelationshipKind.Set5ParentChild, Is.EqualTo(1));

            Assert.That((int)LanguageRetirementReason.Change, Is.EqualTo(0));
            Assert.That((int)LanguageRetirementReason.Duplicate, Is.EqualTo(1));
            Assert.That((int)LanguageRetirementReason.Nonexistent, Is.EqualTo(2));
            Assert.That((int)LanguageRetirementReason.Split, Is.EqualTo(3));
            Assert.That((int)LanguageRetirementReason.Merge, Is.EqualTo(4));
        });
    }

    [Test]
    public void OperationEnumValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)LanguageLookupStatus.NotFound, Is.EqualTo(0));
            Assert.That((int)LanguageLookupStatus.Found, Is.EqualTo(1));
            Assert.That((int)LanguageLookupStatus.Ambiguous, Is.EqualTo(2));
            Assert.That((int)LanguageLookupStatus.Reserved, Is.EqualTo(3));
            Assert.That((int)LanguageLookupStatus.Retired, Is.EqualTo(4));
            Assert.That((int)LanguageLookupStatus.Invalid, Is.EqualTo(5));

            Assert.That((int)LanguageMatchKind.None, Is.EqualTo(0));
            Assert.That((int)LanguageMatchKind.Alpha2, Is.EqualTo(1));
            Assert.That((int)LanguageMatchKind.Set2Terminology, Is.EqualTo(2));
            Assert.That((int)LanguageMatchKind.Set2Bibliographic, Is.EqualTo(3));
            Assert.That((int)LanguageMatchKind.Set3, Is.EqualTo(4));
            Assert.That((int)LanguageMatchKind.Set5, Is.EqualTo(5));
            Assert.That((int)LanguageMatchKind.RetiredSet3, Is.EqualTo(6));
            Assert.That((int)LanguageMatchKind.IanaAlias, Is.EqualTo(7));
            Assert.That((int)LanguageMatchKind.ExactName, Is.EqualTo(8));
            Assert.That((int)LanguageMatchKind.NormalizedName, Is.EqualTo(9));
            Assert.That((int)LanguageMatchKind.PrivateUse, Is.EqualTo(10));

            Assert.That((int)LanguageSearchMode.Exact, Is.EqualTo(0));
            Assert.That((int)LanguageSearchMode.Prefix, Is.EqualTo(1));
            Assert.That((int)LanguageSearchMode.Contains, Is.EqualTo(2));

            Assert.That((int)MacrolanguageNormalizationPolicy.Preserve, Is.EqualTo(0));
            Assert.That((int)MacrolanguageNormalizationPolicy.FoldToMacrolanguage, Is.EqualTo(1));
        });
    }

    [Test]
    public void NormalizationValidationAndTagEnumValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)LanguageNormalizationStatus.Success, Is.EqualTo(0));
            Assert.That((int)LanguageNormalizationStatus.AlreadyCanonical, Is.EqualTo(1));
            Assert.That((int)LanguageNormalizationStatus.NotFound, Is.EqualTo(2));
            Assert.That((int)LanguageNormalizationStatus.Invalid, Is.EqualTo(3));
            Assert.That((int)LanguageNormalizationStatus.Ambiguous, Is.EqualTo(4));
            Assert.That((int)LanguageNormalizationStatus.TargetUnavailable, Is.EqualTo(5));
            Assert.That((int)LanguageNormalizationStatus.Split, Is.EqualTo(6));
            Assert.That((int)LanguageNormalizationStatus.NoReplacement, Is.EqualTo(7));
            Assert.That((int)LanguageNormalizationStatus.ReplacementCycle, Is.EqualTo(8));
            Assert.That((int)LanguageNormalizationStatus.IncompatibleEntity, Is.EqualTo(9));

            Assert.That((int)LanguageNormalizationStep.TrimWhitespace, Is.EqualTo(0));
            Assert.That((int)LanguageNormalizationStep.NormalizeCase, Is.EqualTo(1));
            Assert.That((int)LanguageNormalizationStep.ConvertCodeForm, Is.EqualTo(2));
            Assert.That((int)LanguageNormalizationStep.ReplaceDeprecated, Is.EqualTo(3));
            Assert.That((int)LanguageNormalizationStep.ReplaceRetired, Is.EqualTo(4));
            Assert.That((int)LanguageNormalizationStep.FoldMacrolanguage, Is.EqualTo(5));
            Assert.That((int)LanguageNormalizationStep.ReplaceGrandfathered, Is.EqualTo(6));
            Assert.That((int)LanguageNormalizationStep.ReplaceSubtag, Is.EqualTo(7));
            Assert.That((int)LanguageNormalizationStep.RemoveSuppressScript, Is.EqualTo(8));
            Assert.That((int)LanguageNormalizationStep.NormalizeTagCasing, Is.EqualTo(9));
            Assert.That((int)LanguageNormalizationStep.NormalizeSeparators, Is.EqualTo(10));

            Assert.That((int)LanguageValidationSeverity.Error, Is.EqualTo(0));
            Assert.That((int)LanguageValidationSeverity.Warning, Is.EqualTo(1));
            Assert.That((int)LanguageValidationError.None, Is.EqualTo(0));
            Assert.That((int)LanguageValidationError.NullInput, Is.EqualTo(1));
            Assert.That((int)LanguageValidationError.EmptyInput, Is.EqualTo(2));
            Assert.That((int)LanguageValidationError.InvalidLength, Is.EqualTo(3));
            Assert.That((int)LanguageValidationError.NonAscii, Is.EqualTo(4));
            Assert.That((int)LanguageValidationError.InvalidCharacter, Is.EqualTo(5));
            Assert.That((int)LanguageValidationError.InvalidSeparator, Is.EqualTo(6));
            Assert.That((int)LanguageValidationError.UnknownCode, Is.EqualTo(7));
            Assert.That((int)LanguageValidationError.UnassignedCode, Is.EqualTo(8));
            Assert.That((int)LanguageValidationError.ReservedCode, Is.EqualTo(9));
            Assert.That((int)LanguageValidationError.RetiredCode, Is.EqualTo(10));
            Assert.That((int)LanguageValidationError.DeprecatedCode, Is.EqualTo(11));
            Assert.That((int)LanguageValidationError.WrongSet, Is.EqualTo(12));
            Assert.That((int)LanguageValidationError.WrongCodeForm, Is.EqualTo(13));
            Assert.That((int)LanguageValidationError.DisallowedScope, Is.EqualTo(14));
            Assert.That((int)LanguageValidationError.DisallowedType, Is.EqualTo(15));
            Assert.That((int)LanguageValidationError.SpecialCodeNotAllowed, Is.EqualTo(16));
            Assert.That((int)LanguageValidationError.GroupCodeNotAllowed, Is.EqualTo(17));
            Assert.That((int)LanguageValidationError.PrivateUseNotAllowed, Is.EqualTo(18));
            Assert.That((int)LanguageValidationError.NonCanonical, Is.EqualTo(19));
            Assert.That((int)LanguageValidationError.InvalidTag, Is.EqualTo(20));
            Assert.That((int)LanguageValidationError.DuplicateSubtag, Is.EqualTo(21));
            Assert.That((int)LanguageValidationError.PrefixMismatch, Is.EqualTo(22));

            Assert.That((int)LanguageSubtagType.Language, Is.EqualTo(0));
            Assert.That((int)LanguageSubtagType.Extlang, Is.EqualTo(1));
            Assert.That((int)LanguageSubtagType.Script, Is.EqualTo(2));
            Assert.That((int)LanguageSubtagType.Region, Is.EqualTo(3));
            Assert.That((int)LanguageSubtagType.Variant, Is.EqualTo(4));
            Assert.That((int)LanguageSubtagType.Extension, Is.EqualTo(5));
            Assert.That((int)LanguageSubtagType.PrivateUse, Is.EqualTo(6));
            Assert.That((int)LanguageSubtagType.Grandfathered, Is.EqualTo(7));
            Assert.That((int)LanguageSubtagType.Redundant, Is.EqualTo(8));

            Assert.That((int)LanguageTagMatchStatus.NoMatch, Is.EqualTo(0));
            Assert.That((int)LanguageTagMatchStatus.Matched, Is.EqualTo(1));
            Assert.That((int)LanguageTagMatchStatus.InvalidRange, Is.EqualTo(2));
            Assert.That((int)LanguageTagMatchStatus.InvalidTag, Is.EqualTo(3));
        });
    }

    [Test]
    public void GeneratedRowsUsePublicClassificationEnums()
    {
        LanguageRow? english = Array.Find(GeneratedLanguageEntries.Rows, row => row.Set3Code == "eng");
        CodeIndexSeedRow? englishAlpha2 = Array.Find(
            GeneratedLanguageEntries.CodeIndexSeeds,
            row => row.Value == "en");
        IanaSubtagRow? oxfordEnglish = Array.Find(
            GeneratedIanaRegistry.Rows,
            row => row.Value == "en-GB-oed");

        Assert.Multiple(() =>
        {
            Assert.That(english?.Scope, Is.EqualTo(LanguageScope.Individual));
            Assert.That(english?.LanguageType, Is.EqualTo(LanguageType.Living));
            Assert.That(english?.Status, Is.EqualTo(LanguageCodeStatus.Active));
            Assert.That(englishAlpha2?.Form, Is.EqualTo(LanguageCodeForm.Alpha2));
            Assert.That(englishAlpha2?.Set, Is.EqualTo(Iso639Set.Set1));
            Assert.That(oxfordEnglish?.RecordType, Is.EqualTo(LanguageSubtagType.Grandfathered));
        });
    }
}
