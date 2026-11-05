using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeCompositionTests
{
    [Test]
    public void ComposeCanonicalPair_ComposesCanonicalTablePairs()
    {
        Assert.That(UnicodeNormalization.ComposeCanonicalPair(0x0041, 0x0300), Is.EqualTo(0x00C0));
        Assert.That(UnicodeNormalization.ComposeCanonicalPair(0x0055, 0x0308), Is.EqualTo(0x00DC));
        Assert.That(UnicodeNormalization.ComposeCanonicalPair(0x00DC, 0x0300), Is.EqualTo(0x01DB));
    }

    [Test]
    public void ComposeCanonicalPair_ComposesHangulPairs()
    {
        Assert.That(UnicodeNormalization.ComposeCanonicalPair(0x1100, 0x1161), Is.EqualTo(0xAC00));
        Assert.That(UnicodeNormalization.ComposeCanonicalPair(0xAC00, 0x11A8), Is.EqualTo(0xAC01));
    }

    [Test]
    public void ComposeCanonicalPair_SkipsCompatibilityMappingsAndCompositionExclusions()
    {
        Assert.That(UnicodeNormalization.ComposeCanonicalPair(0x0032, 0x0000), Is.EqualTo(-1));
        Assert.That(UnicodeNormalization.IsCompositionExcluded(0x0958), Is.True);
        Assert.That(UnicodeNormalization.ComposeCanonicalPair(0x0915, 0x093C), Is.EqualTo(-1));
    }

    [Test]
    public void ComposeCanonicalCodePoints_ComposesOrderedSequences()
    {
        Assert.That(
            UnicodeNormalization.ComposeCanonicalCodePoints(new int[] { 0x0055, 0x0308, 0x0300 }),
            Is.EqualTo(new int[] { 0x01DB }));
        Assert.That(
            UnicodeNormalization.ComposeCanonicalCodePoints(new int[] { 0x1100, 0x1161, 0x11A8 }),
            Is.EqualTo(new int[] { 0xAC01 }));
    }

    [Test]
    public void ComposeCanonicalCodePoints_RespectsBlocking()
    {
        Assert.That(
            UnicodeNormalization.ComposeCanonicalCodePoints(new int[] { 0x0041, 0x0301, 0x0300 }),
            Is.EqualTo(new int[] { 0x00C1, 0x0300 }));
    }

    [Test]
    public void ComposeCanonical_ComposesText()
    {
        Assert.That(UnicodeNormalization.ComposeCanonical("U\u0308\u0300\u1100\u1161\u11A8"), Is.EqualTo("\u01DB\uAC01"));
    }

    [Test]
    public void ComposeCanonical_RejectsNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => UnicodeNormalization.ComposeCanonical(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodeNormalization.ComposeCanonicalCodePoints(null!));
    }
}
