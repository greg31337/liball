using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeIdentifierProfileRegressionTests
{
    [Test]
    public void DefaultAndNormalizedProfiles_DifferOnlyByNormalizationRequirementForFixtureIdentifier()
    {
        string text = "A\u0300";

        UnicodeIdentifierValidationResult defaultResult = new UnicodeIdentifiers().ValidateIdentifier(text, new UnicodeProfiles().CreateDefaultIdentifier());
        UnicodeIdentifierValidationResult normalizedResult = new UnicodeIdentifiers().ValidateIdentifier(text, new UnicodeProfiles().CreateNormalizedIdentifier());

        Assert.That(defaultResult.IsValid, Is.True);
        Assert.That(normalizedResult.IsValid, Is.False);
        Assert.That(normalizedResult.Status, Is.EqualTo(UnicodeIdentifierStatus.NeedsNormalization));
        Assert.That(new UnicodeIdentifiers().NormalizeIdentifier(text, new UnicodeProfiles().CreateNormalizedIdentifier()), Is.EqualTo("\u00C0"));
    }

    [Test]
    public void ImmutableProfile_DoesNotRelaxPatternSyntaxOrWhitespace()
    {
        UnicodeIdentifierProfile profile = new UnicodeProfiles().CreateImmutableIdentifier();

        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("\U00010000", profile).IsValid, Is.True);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("A ", profile).IsValid, Is.False);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("A!", profile).IsValid, Is.False);
    }

    [Test]
    public void ImmutableProfile_RejectsControlsPrivateUseAndNoncharacters()
    {
        UnicodeIdentifierProfile profile = new UnicodeProfiles().CreateImmutableIdentifier();

        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("\u0000", profile).IsValid, Is.False);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("\uE000", profile).IsValid, Is.False);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("\uFDD0", profile).IsValid, Is.False);
        Assert.That(new UnicodeIdentifiers().ValidateIdentifier("0\U0001F600", profile).IsValid, Is.True);
    }

    [Test]
    public void CaseInsensitiveProfile_UsesCompatibilityFoldKeys()
    {
        UnicodeIdentifierProfile profile = new UnicodeProfiles().CreateCaseInsensitiveIdentifier();

        Assert.That(new UnicodeIdentifiers().GetIdentifierCompareKey("\u00B2A\u00DF", profile), Is.EqualTo("2ass"));
        Assert.That(new UnicodeIdentifiers().EqualsIdentifier("A\u00DF", "ass", profile), Is.True);
        Assert.That(new UnicodeIdentifiers().EqualsIdentifier("A\u00DF", "ass", new UnicodeProfiles().CreateDefaultIdentifier()), Is.False);
    }

    [Test]
    public void PatternPropertyClassifiers_MatchIdentifierFixtureTables()
    {
        Assert.That(new UnicodeIdentifiers().IsPatternSyntax(0x0021), Is.True);
        Assert.That(new UnicodeIdentifiers().IsPatternSyntax(0x0041), Is.False);
        Assert.That(new UnicodeIdentifiers().IsPatternWhiteSpace(0x0020), Is.True);
        Assert.That(new UnicodeIdentifiers().IsPatternWhiteSpace(0x0041), Is.False);
    }
}
