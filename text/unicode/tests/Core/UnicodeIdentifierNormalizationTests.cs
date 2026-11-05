using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeIdentifierNormalizationTests
{
    [Test]
    public void NormalizeIdentifier_ReturnsOriginalTextForProfilesWithoutNormalization()
    {
        string decomposed = "A\u0300";

        Assert.That(
            new UnicodeIdentifiers().NormalizeIdentifier(decomposed, new UnicodeProfiles().CreateDefaultIdentifier()),
            Is.EqualTo(decomposed));
        Assert.That(
            new UnicodeIdentifiers().NormalizeIdentifier(decomposed, new UnicodeProfiles().CreateImmutableIdentifier()),
            Is.EqualTo(decomposed));
    }

    [Test]
    public void NormalizeIdentifier_UsesProfileNormalizationForm()
    {
        Assert.That(
            new UnicodeIdentifiers().NormalizeIdentifier("A\u0300", new UnicodeProfiles().CreateNormalizedIdentifier()),
            Is.EqualTo("\u00C0"));
        Assert.That(
            new UnicodeIdentifiers().NormalizeIdentifier("\u00B2", new UnicodeProfiles().CreateCaseInsensitiveIdentifier()),
            Is.EqualTo("2"));
    }

    [Test]
    public void GetIdentifierCompareKey_NormalizesCaseSensitiveProfilesWithoutFolding()
    {
        UnicodeIdentifierProfile profile = new UnicodeProfiles().CreateNormalizedIdentifier();

        Assert.That(new UnicodeIdentifiers().GetIdentifierCompareKey("A\u0300", profile), Is.EqualTo("\u00C0"));
        Assert.That(new UnicodeIdentifiers().GetIdentifierCompareKey("A\u00DF", profile), Is.EqualTo("A\u00DF"));
    }

    [Test]
    public void GetIdentifierCompareKey_FoldsCaseInsensitiveProfiles()
    {
        UnicodeIdentifierProfile profile = new UnicodeProfiles().CreateCaseInsensitiveIdentifier();

        Assert.That(new UnicodeIdentifiers().GetIdentifierCompareKey("\u00B2A\u00DF", profile), Is.EqualTo("2ass"));
        Assert.That(new UnicodeIdentifiers().GetIdentifierCompareKey("A\u0300", profile), Is.EqualTo("\u00E0"));
    }

    [Test]
    public void EqualsIdentifier_UsesProfileCompareKeys()
    {
        Assert.That(
            new UnicodeIdentifiers().EqualsIdentifier("A\u00DF", "ass", new UnicodeProfiles().CreateDefaultIdentifier()),
            Is.False);
        Assert.That(
            new UnicodeIdentifiers().EqualsIdentifier("A\u00DF", "ass", new UnicodeProfiles().CreateCaseInsensitiveIdentifier()),
            Is.True);
        Assert.That(
            new UnicodeIdentifiers().EqualsIdentifier("A\u0300", "\u00C0", new UnicodeProfiles().CreateNormalizedIdentifier()),
            Is.True);
    }

    [Test]
    public void IdentifierNormalizationHelpers_RejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodeIdentifiers().NormalizeIdentifier(null!, new UnicodeProfiles().CreateDefaultIdentifier()));
        Assert.Throws<ArgumentNullException>(() => new UnicodeIdentifiers().NormalizeIdentifier("A", null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeIdentifiers().GetIdentifierCompareKey(null!, new UnicodeProfiles().CreateDefaultIdentifier()));
        Assert.Throws<ArgumentNullException>(() => new UnicodeIdentifiers().GetIdentifierCompareKey("A", null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodeIdentifiers().EqualsIdentifier(null!, "A", new UnicodeProfiles().CreateDefaultIdentifier()));
        Assert.Throws<ArgumentNullException>(() => new UnicodeIdentifiers().EqualsIdentifier("A", null!, new UnicodeProfiles().CreateDefaultIdentifier()));
        Assert.Throws<ArgumentNullException>(() => new UnicodeIdentifiers().EqualsIdentifier("A", "A", null!));
    }
}
