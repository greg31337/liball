namespace uri.Tests;

public class IriGrammarTests
{
    [TestCase(0x00A0)]
    [TestCase(0xD7FF)]
    [TestCase(0xF900)]
    [TestCase(0xFDCF)]
    [TestCase(0xFDF0)]
    [TestCase(0xFFEF)]
    [TestCase(0x10000)]
    [TestCase(0x1FFFD)]
    [TestCase(0x20000)]
    [TestCase(0x2FFFD)]
    [TestCase(0xD0000)]
    [TestCase(0xDFFFD)]
    [TestCase(0xE1000)]
    [TestCase(0xEFFFD)]
    public void UcsCharacterAcceptsRfc3987Ranges(int scalarValue)
    {
        Assert.That(IriGrammar.IsUcsCharacter(scalarValue), Is.True);
    }

    [TestCase(-1)]
    [TestCase(0x009F)]
    [TestCase(0xD800)]
    [TestCase(0xDFFF)]
    [TestCase(0xE000)]
    [TestCase(0xF8FF)]
    [TestCase(0xFDD0)]
    [TestCase(0xFDEF)]
    [TestCase(0xFFF0)]
    [TestCase(0xFFFF)]
    [TestCase(0x1FFFE)]
    [TestCase(0x1FFFF)]
    [TestCase(0xDFFFE)]
    [TestCase(0xDFFFF)]
    [TestCase(0xE0000)]
    [TestCase(0xE0FFF)]
    [TestCase(0xEFFFE)]
    [TestCase(0xEFFFF)]
    [TestCase(0xF0000)]
    [TestCase(0x10FFFD)]
    [TestCase(0x10FFFF)]
    [TestCase(0x110000)]
    public void UcsCharacterRejectsExcludedAndInvalidScalars(int scalarValue)
    {
        Assert.That(IriGrammar.IsUcsCharacter(scalarValue), Is.False);
    }

    [TestCase(0xE000)]
    [TestCase(0xF8FF)]
    [TestCase(0xF0000)]
    [TestCase(0xFFFFD)]
    [TestCase(0x100000)]
    [TestCase(0x10FFFD)]
    public void PrivateCharacterAcceptsOnlyRfc3987PrivateRanges(int scalarValue)
    {
        Assert.That(IriGrammar.IsPrivateCharacter(scalarValue), Is.True);
    }

    [TestCase(0xDFFF)]
    [TestCase(0xF900)]
    [TestCase(0xEFFFF)]
    [TestCase(0xFFFFE)]
    [TestCase(0xFFFFF)]
    [TestCase(0x10FFFE)]
    [TestCase(0x10FFFF)]
    public void PrivateCharacterRejectsNeighborsAndPlaneNoncharacters(int scalarValue)
    {
        Assert.That(IriGrammar.IsPrivateCharacter(scalarValue), Is.False);
    }

    [Test]
    public void InternationalizedUnreservedExtendsAsciiUnreservedWithUcsCharacter()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IriGrammar.IsInternationalizedUnreserved('a'), Is.True);
            Assert.That(IriGrammar.IsInternationalizedUnreserved('-'), Is.True);
            Assert.That(IriGrammar.IsInternationalizedUnreserved(0x00A0), Is.True);
            Assert.That(IriGrammar.IsInternationalizedUnreserved(':'), Is.False);
            Assert.That(IriGrammar.IsInternationalizedUnreserved(0x009F), Is.False);
            Assert.That(IriGrammar.IsInternationalizedUnreserved(0xE000), Is.False);
        });
    }

    [Test]
    public void ComponentTablesExtendOnlyTheirRfc3987Productions()
    {
        const int ucsCharacter = 0x00A0;
        const int privateCharacter = 0xE000;

        Assert.Multiple(() =>
        {
            Assert.That(IriGrammar.IsUserInfoCharacter(ucsCharacter), Is.True);
            Assert.That(IriGrammar.IsUserInfoCharacter(':'), Is.True);
            Assert.That(IriGrammar.IsUserInfoCharacter('@'), Is.False);
            Assert.That(IriGrammar.IsRegisteredNameCharacter(ucsCharacter), Is.True);
            Assert.That(IriGrammar.IsRegisteredNameCharacter('!'), Is.True);
            Assert.That(IriGrammar.IsRegisteredNameCharacter(':'), Is.False);
            Assert.That(IriGrammar.IsPathSegmentCharacter(ucsCharacter), Is.True);
            Assert.That(IriGrammar.IsPathSegmentCharacter(':'), Is.True);
            Assert.That(IriGrammar.IsPathSegmentCharacter('@'), Is.True);
            Assert.That(IriGrammar.IsPathSegmentCharacter('/'), Is.False);
            Assert.That(IriGrammar.IsPathNoSchemeSegmentCharacter('@'), Is.True);
            Assert.That(IriGrammar.IsPathNoSchemeSegmentCharacter(':'), Is.False);
            Assert.That(IriGrammar.IsPathCharacter('/'), Is.True);
            Assert.That(IriGrammar.IsPathCharacter('?'), Is.False);
            Assert.That(IriGrammar.IsQueryCharacter('/'), Is.True);
            Assert.That(IriGrammar.IsQueryCharacter('?'), Is.True);
            Assert.That(IriGrammar.IsQueryCharacter(privateCharacter), Is.True);
            Assert.That(IriGrammar.IsFragmentCharacter('/'), Is.True);
            Assert.That(IriGrammar.IsFragmentCharacter('?'), Is.True);
            Assert.That(IriGrammar.IsFragmentCharacter(privateCharacter), Is.False);
        });
    }

    [Test]
    public void PercentAndInvalidScalarsRequireSeparateScannerHandling()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IriGrammar.IsUserInfoCharacter('%'), Is.False);
            Assert.That(IriGrammar.IsRegisteredNameCharacter('%'), Is.False);
            Assert.That(IriGrammar.IsPathSegmentCharacter('%'), Is.False);
            Assert.That(IriGrammar.IsPathCharacter('%'), Is.False);
            Assert.That(IriGrammar.IsQueryCharacter('%'), Is.False);
            Assert.That(IriGrammar.IsFragmentCharacter('%'), Is.False);
            Assert.That(IriGrammar.IsQueryCharacter(0xD800), Is.False);
            Assert.That(IriGrammar.IsFragmentCharacter(0x110000), Is.False);
            Assert.That(IriGrammar.IsPathCharacter(-1), Is.False);
        });
    }
}
