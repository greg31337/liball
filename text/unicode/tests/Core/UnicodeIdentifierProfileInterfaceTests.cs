using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeIdentifierProfileInterfaceTests
{
    [Test]
    public void ValidateIdentifier_AcceptsCustomBehavioralProfile()
    {
        IUnicodeIdentifierProfile profile = new DollarIdentifierProfile();

        UnicodeIdentifierValidationResult valid = new UnicodeIdentifiers().ValidateIdentifier("$12", profile);
        UnicodeIdentifierValidationResult invalid = new UnicodeIdentifiers().ValidateIdentifier("A12", profile);

        Assert.That(valid.IsValid, Is.True);
        Assert.That(invalid.Status, Is.EqualTo(UnicodeIdentifierStatus.InvalidStart));
    }

    [Test]
    public void PreparationAndComparison_DelegateToBehavioralProfile()
    {
        IUnicodeIdentifierProfile profile = new DollarIdentifierProfile();

        Assert.That(new UnicodeIdentifiers().NormalizeIdentifier("value", profile), Is.EqualTo("prepared:value"));
        Assert.That(new UnicodeIdentifiers().GetIdentifierCompareKey("Value", profile), Is.EqualTo("VALUE"));
        Assert.That(new UnicodeIdentifiers().EqualsIdentifier("Value", "value", profile), Is.True);
    }

    [Test]
    public void SecurityCheck_UsesInterfaceProfileForIdentifierValidation()
    {
        IUnicodeIdentifierProfile profile = new DollarIdentifierProfile();

        UnicodeSecurityCheckResult result = new UnicodeSecurity().CheckIdentifier("$12", profile);

        Assert.That(result.IdentifierValidation.IsValid, Is.True);
    }

    [Test]
    public void BuiltInProfile_ImplementsBehavioralContract()
    {
        IUnicodeIdentifierProfile profile = new UnicodeProfiles().CreateNormalizedIdentifier();

        Assert.That(profile.AllowsStart(0x0041), Is.True);
        Assert.That(profile.DisallowsPatternSyntax(), Is.True);
        Assert.That(profile.Normalize("A\u0300"), Is.EqualTo("\u00C0"));
    }

    class DollarIdentifierProfile : IUnicodeIdentifierProfile
    {
        public bool AllowsStart(int codePoint)
        {
            return codePoint == 0x0024;
        }

        public bool AllowsContinue(int codePoint)
        {
            return codePoint >= 0x0030 && codePoint <= 0x0039;
        }

        public bool AllowsUnassignedCodePoints()
        {
            return false;
        }

        public bool DisallowsPatternSyntax()
        {
            return false;
        }

        public bool DisallowsPatternWhiteSpace()
        {
            return false;
        }

        public bool IsNormalizationSatisfied(string text)
        {
            return true;
        }

        public string Normalize(string text)
        {
            return "prepared:" + text;
        }

        public string GetCompareKey(string text)
        {
            return text.ToUpperInvariant();
        }
    }
}
