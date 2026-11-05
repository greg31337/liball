using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeSecurityRestrictionTests
{
    [TestCase("AZ09", UnicodeRestrictionLevel.Ascii)]
    [TestCase("\u03B1\u03B2", UnicodeRestrictionLevel.SingleScriptRestrictive)]
    [TestCase("\u4E00\uAC00", UnicodeRestrictionLevel.SingleScriptRestrictive)]
    [TestCase("A\u4E00\uAC00", UnicodeRestrictionLevel.HighlyRestrictive)]
    [TestCase("A\u05D0", UnicodeRestrictionLevel.ModeratelyRestrictive)]
    [TestCase("A\u0531", UnicodeRestrictionLevel.ModeratelyRestrictive)]
    [TestCase("A\u03B1", UnicodeRestrictionLevel.MinimallyRestrictive)]
    [TestCase("A\u03B1\u0628", UnicodeRestrictionLevel.MinimallyRestrictive)]
    [TestCase("\U0001F600", UnicodeRestrictionLevel.Unrestricted)]
    [TestCase("", UnicodeRestrictionLevel.Unrestricted)]
    public void GetRestrictionLevel_ReturnsRepresentativeLevels(string text, UnicodeRestrictionLevel expected)
    {
        Assert.That(new UnicodeSecurity().GetRestrictionLevel(text), Is.EqualTo(expected));
    }

    [Test]
    public void IsAllowedByIdentifierStatus_UsesGeneratedAllowedRanges()
    {
        Assert.That(UnicodeSecurity.IsAllowedByIdentifierStatus("AZ09"), Is.True);
        Assert.That(UnicodeSecurity.IsAllowedByIdentifierStatus("\u03B1\u0628\u4E00\uAC00"), Is.True);
        Assert.That(UnicodeSecurity.IsAllowedByIdentifierStatus("\U0001F600"), Is.False);
        Assert.That(UnicodeSecurity.IsAllowedByIdentifierStatus(""), Is.False);
    }

    [Test]
    public void IsAsciiOnly_RequiresAtLeastOneAsciiScalar()
    {
        Assert.That(UnicodeSecurity.IsAsciiOnly("AZ09"), Is.True);
        Assert.That(UnicodeSecurity.IsAsciiOnly("\u0370"), Is.False);
        Assert.That(UnicodeSecurity.IsAsciiOnly(""), Is.False);
    }

    [Test]
    public void RestrictionLevels_UseAugmentedScriptExtensionSets()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        Assert.That(security.GetRestrictionLevel("\u306D\u30AC"), Is.EqualTo(UnicodeRestrictionLevel.SingleScriptRestrictive));
        Assert.That(security.GetRestrictionLevel("A\u306D\u30AC"), Is.EqualTo(UnicodeRestrictionLevel.HighlyRestrictive));
        Assert.That(security.GetRestrictionLevel("A\u0411"), Is.EqualTo(UnicodeRestrictionLevel.MinimallyRestrictive));
    }

    [Test]
    public void SecurityProfileAndRestrictionLevels_HandleAdversarialBoundaries()
    {
        UnicodeSecurity security = new UnicodeSecurity();

        Assert.That(security.IsAllowedByGeneralSecurityProfile("\u00C5"), Is.True);
        Assert.That(security.IsAllowedByGeneralSecurityProfile("A\u030A"), Is.True);
        Assert.That(security.IsAllowedByGeneralSecurityProfile("A\u00AD"), Is.False);
        Assert.That(security.GetRestrictionLevel("A\u00AD"), Is.EqualTo(UnicodeRestrictionLevel.Unrestricted));
        Assert.That(security.GetRestrictionLevel("A0"), Is.EqualTo(UnicodeRestrictionLevel.Ascii));
        Assert.That(security.GetRestrictionLevel("\u3006\u5207"), Is.EqualTo(UnicodeRestrictionLevel.SingleScriptRestrictive));
    }

    [Test]
    public void RestrictionLevelHelpers_RejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeSecurity().GetRestrictionLevel(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeSecurity().IsAllowedByGeneralSecurityProfile(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodeSecurity.IsAllowedByIdentifierStatus(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodeSecurity.IsAsciiOnly(null!));
    }
}
