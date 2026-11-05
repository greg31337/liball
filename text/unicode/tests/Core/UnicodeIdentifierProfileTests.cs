using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeIdentifierProfileTests
{
    [Test]
    public void CreateDefault_ReturnsXidCaseSensitiveProfile()
    {
        UnicodeIdentifierProfile profile = new UnicodeProfiles().CreateDefaultIdentifier();

        Assert.That(profile.Name, Is.EqualTo("Default"));
        Assert.That(profile.UseXidProperties, Is.True);
        Assert.That(profile.AllowUnassignedCodePoints, Is.False);
        Assert.That(profile.RequireNormalization, Is.False);
        Assert.That(profile.NormalizationForm, Is.EqualTo(UnicodeNormalizationForm.Nfc));
        Assert.That(profile.CaseInsensitive, Is.False);
        Assert.That(profile.CompareKeyMode, Is.EqualTo(UnicodeCompareKeyMode.OrdinalFold));
        Assert.That(profile.DisallowPatternSyntax, Is.True);
        Assert.That(profile.DisallowPatternWhiteSpace, Is.True);
    }

    [Test]
    public void CreateImmutable_ReturnsUax31ImmutableProfileWithoutNormalization()
    {
        UnicodeIdentifierProfile profile = new UnicodeProfiles().CreateImmutableIdentifier();

        Assert.That(profile.Name, Is.EqualTo("Immutable"));
        Assert.That(profile.UseXidProperties, Is.False);
        Assert.That(profile.AllowUnassignedCodePoints, Is.True);
        Assert.That(profile.RequireNormalization, Is.False);
        Assert.That(profile.CaseInsensitive, Is.False);
        Assert.That(profile.DisallowPatternSyntax, Is.True);
        Assert.That(profile.DisallowPatternWhiteSpace, Is.True);
        Assert.That(profile.AllowsStart(0x0030), Is.True);
        Assert.That(profile.AllowsContinue(0x1F600), Is.True);
        Assert.That(profile.AllowsStart(0x0000), Is.False);
        Assert.That(profile.AllowsStart(0xE000), Is.False);
        Assert.That(profile.AllowsStart(0xFDD0), Is.False);
    }

    [Test]
    public void CreateNormalized_ReturnsNfcFilteredProfile()
    {
        UnicodeIdentifierProfile profile = new UnicodeProfiles().CreateNormalizedIdentifier();

        Assert.That(profile.Name, Is.EqualTo("Normalized"));
        Assert.That(profile.UseXidProperties, Is.True);
        Assert.That(profile.AllowUnassignedCodePoints, Is.False);
        Assert.That(profile.RequireNormalization, Is.True);
        Assert.That(profile.NormalizationForm, Is.EqualTo(UnicodeNormalizationForm.Nfc));
        Assert.That(profile.CaseInsensitive, Is.False);
        Assert.That(profile.CompareKeyMode, Is.EqualTo(UnicodeCompareKeyMode.NfcFold));
    }

    [Test]
    public void CreateCaseInsensitive_ReturnsNfkcFoldedProfile()
    {
        UnicodeIdentifierProfile profile = new UnicodeProfiles().CreateCaseInsensitiveIdentifier();

        Assert.That(profile.Name, Is.EqualTo("CaseInsensitive"));
        Assert.That(profile.UseXidProperties, Is.True);
        Assert.That(profile.AllowUnassignedCodePoints, Is.False);
        Assert.That(profile.RequireNormalization, Is.True);
        Assert.That(profile.NormalizationForm, Is.EqualTo(UnicodeNormalizationForm.Nfkc));
        Assert.That(profile.CaseInsensitive, Is.True);
        Assert.That(profile.CompareKeyMode, Is.EqualTo(UnicodeCompareKeyMode.NfkcFold));
    }

    [Test]
    public void FactoryMethods_ReturnIndependentProfileInstances()
    {
        UnicodeIdentifierProfile first = new UnicodeProfiles().CreateDefaultIdentifier();
        UnicodeIdentifierProfile second = new UnicodeProfiles().CreateDefaultIdentifier();

        first.Name = "Changed";

        Assert.That(second.Name, Is.EqualTo("Default"));
    }

    [Test]
    public void ProfilePredicates_UseSelectedIdentifierTables()
    {
        UnicodeIdentifierProfile defaultProfile = new UnicodeProfiles().CreateDefaultIdentifier();
        UnicodeIdentifierProfile immutableProfile = new UnicodeProfiles().CreateImmutableIdentifier();

        Assert.That(defaultProfile.AllowsStart(0x0041), Is.True);
        Assert.That(defaultProfile.AllowsStart(0x0030), Is.False);
        Assert.That(defaultProfile.AllowsContinue(0x0030), Is.True);
        Assert.That(immutableProfile.AllowsStart(0x0061), Is.True);
        Assert.That(immutableProfile.AllowsContinue(0x0301), Is.True);
    }

    [Test]
    public void IsNormalizationSatisfied_AppliesOnlyWhenProfileRequiresIt()
    {
        UnicodeIdentifierProfile defaultProfile = new UnicodeProfiles().CreateDefaultIdentifier();
        UnicodeIdentifierProfile normalizedProfile = new UnicodeProfiles().CreateNormalizedIdentifier();
        string decomposed = "U\u0308\u0300";

        Assert.That(defaultProfile.IsNormalizationSatisfied(decomposed), Is.True);
        Assert.That(normalizedProfile.IsNormalizationSatisfied(decomposed), Is.False);
        Assert.That(normalizedProfile.IsNormalizationSatisfied(new UnicodeNormalization().Normalize(decomposed, UnicodeNormalizationForm.Nfc)), Is.True);
    }

    [Test]
    public void Constructor_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentException>(() => new UnicodeIdentifierProfile("", true, false, false, UnicodeNormalizationForm.Nfc, false, UnicodeCompareKeyMode.OrdinalFold, true, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeIdentifierProfile("Bad", true, false, false, (UnicodeNormalizationForm)99, false, UnicodeCompareKeyMode.OrdinalFold, true, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeIdentifierProfile("Bad", true, false, false, UnicodeNormalizationForm.Nfc, false, (UnicodeCompareKeyMode)99, true, true));
        Assert.Throws<ArgumentNullException>(() => new UnicodeProfiles().CreateNormalizedIdentifier().IsNormalizationSatisfied(null!));
    }
}
