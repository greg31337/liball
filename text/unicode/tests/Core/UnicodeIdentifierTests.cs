using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeIdentifierTests
{
    [TestCase(0x0041)]
    [TestCase(0x005A)]
    [TestCase(0x0061)]
    [TestCase(0x007A)]
    public void IsIdStart_ReturnsTrueForRepresentativeStartCharacters(int codePoint)
    {
        Assert.That(new UnicodeIdentifiers().IsIdStart(codePoint), Is.True);
    }

    [TestCase(0x0030)]
    [TestCase(0x005F)]
    [TestCase(0x0301)]
    public void IsIdStart_ReturnsFalseForContinueOnlyCharacters(int codePoint)
    {
        Assert.That(new UnicodeIdentifiers().IsIdStart(codePoint), Is.False);
    }

    [TestCase(0x0030)]
    [TestCase(0x0039)]
    [TestCase(0x0041)]
    [TestCase(0x005F)]
    [TestCase(0x0061)]
    [TestCase(0x0301)]
    public void IsIdContinue_ReturnsTrueForRepresentativeContinueCharacters(int codePoint)
    {
        Assert.That(new UnicodeIdentifiers().IsIdContinue(codePoint), Is.True);
    }

    [TestCase(0x0021)]
    [TestCase(0x0020)]
    [TestCase(0x1F600)]
    public void IsIdContinue_ReturnsFalseForNonIdentifierCharacters(int codePoint)
    {
        Assert.That(new UnicodeIdentifiers().IsIdContinue(codePoint), Is.False);
    }

    [Test]
    public void XidPredicates_UseXidTables()
    {
        Assert.That(new UnicodeIdentifiers().IsXidStart(0x0041), Is.True);
        Assert.That(new UnicodeIdentifiers().IsXidStart(0x0030), Is.False);
        Assert.That(new UnicodeIdentifiers().IsXidContinue(0x0030), Is.True);
        Assert.That(new UnicodeIdentifiers().IsXidContinue(0x0301), Is.True);
        Assert.That(new UnicodeIdentifiers().IsXidContinue(0x0020), Is.False);
    }

    [Test]
    public void IdentifierPredicates_ReturnFalseForInvalidCodePoints()
    {
        Assert.That(new UnicodeIdentifiers().IsIdStart(-1), Is.False);
        Assert.That(new UnicodeIdentifiers().IsIdContinue(0x110000), Is.False);
        Assert.That(new UnicodeIdentifiers().IsXidStart(-1), Is.False);
        Assert.That(new UnicodeIdentifiers().IsXidContinue(0x110000), Is.False);
    }
}
