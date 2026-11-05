using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeHangulTests
{
    [Test]
    public void IsHangulSyllable_ReturnsTrueOnlyForModernHangulSyllableRange()
    {
        Assert.That(new UnicodeHangul().IsHangulSyllable(0xABFF), Is.False);
        Assert.That(new UnicodeHangul().IsHangulSyllable(0xAC00), Is.True);
        Assert.That(new UnicodeHangul().IsHangulSyllable(0xD7A3), Is.True);
        Assert.That(new UnicodeHangul().IsHangulSyllable(0xD7A4), Is.False);
    }

    [Test]
    public void JamoClassifiers_ReturnExpectedModernJamoRanges()
    {
        Assert.That(new UnicodeHangul().IsLeadingJamo(0x1100), Is.True);
        Assert.That(new UnicodeHangul().IsLeadingJamo(0x1112), Is.True);
        Assert.That(new UnicodeHangul().IsLeadingJamo(0x1113), Is.False);
        Assert.That(new UnicodeHangul().IsVowelJamo(0x1161), Is.True);
        Assert.That(new UnicodeHangul().IsVowelJamo(0x1175), Is.True);
        Assert.That(new UnicodeHangul().IsVowelJamo(0x1176), Is.False);
        Assert.That(new UnicodeHangul().IsTrailingJamo(0x11A7), Is.False);
        Assert.That(new UnicodeHangul().IsTrailingJamo(0x11A8), Is.True);
        Assert.That(new UnicodeHangul().IsTrailingJamo(0x11C2), Is.True);
        Assert.That(new UnicodeHangul().IsTrailingJamo(0x11C3), Is.False);
    }

    [Test]
    public void DecomposeSyllable_DecomposesLvAndLvtSyllables()
    {
        Assert.That(new UnicodeHangul().DecomposeSyllable(0xAC00), Is.EqualTo(new int[] { 0x1100, 0x1161 }));
        Assert.That(new UnicodeHangul().DecomposeSyllable(0xAC01), Is.EqualTo(new int[] { 0x1100, 0x1161, 0x11A8 }));
        Assert.That(new UnicodeHangul().DecomposeSyllable(0xB098), Is.EqualTo(new int[] { 0x1102, 0x1161 }));
        Assert.That(new UnicodeHangul().DecomposeSyllable(0xD7A3), Is.EqualTo(new int[] { 0x1112, 0x1175, 0x11C2 }));
    }

    [Test]
    public void DecomposeSyllable_ReturnsEmptyArrayForNonHangulSyllables()
    {
        Assert.That(new UnicodeHangul().DecomposeSyllable(0x0041), Is.Empty);
        Assert.That(new UnicodeHangul().DecomposeSyllable(0x1100), Is.Empty);
    }

    [Test]
    public void IsLvAndIsLvtSyllable_DistinguishTrailingJamoPresence()
    {
        Assert.That(new UnicodeHangul().IsLvSyllable(0xAC00), Is.True);
        Assert.That(new UnicodeHangul().IsLvtSyllable(0xAC00), Is.False);
        Assert.That(new UnicodeHangul().IsLvSyllable(0xAC01), Is.False);
        Assert.That(new UnicodeHangul().IsLvtSyllable(0xAC01), Is.True);
    }

    [Test]
    public void ComposeJamo_ComposesModernJamoIntoSyllables()
    {
        Assert.That(new UnicodeHangul().ComposeJamoPair(0x1100, 0x1161), Is.EqualTo(0xAC00));
        Assert.That(new UnicodeHangul().ComposeJamoSequence(0x1100, 0x1161, 0x11A8), Is.EqualTo(0xAC01));
        Assert.That(new UnicodeHangul().ComposeJamoPair(0x1102, 0x1161), Is.EqualTo(0xB098));
        Assert.That(new UnicodeHangul().ComposeJamoSequence(0x1112, 0x1175, 0x11C2), Is.EqualTo(0xD7A3));
    }

    [Test]
    public void ComposePair_ComposesLeadingVowelAndLvTrailingPairs()
    {
        Assert.That(new UnicodeHangul().ComposePair(0x1100, 0x1161), Is.EqualTo(0xAC00));
        Assert.That(new UnicodeHangul().ComposePair(0xAC00, 0x11A8), Is.EqualTo(0xAC01));
        Assert.That(new UnicodeHangul().ComposePair(0xAC01, 0x11A8), Is.EqualTo(-1));
        Assert.That(new UnicodeHangul().ComposePair(0x0041, 0x1161), Is.EqualTo(-1));
        Assert.That(new UnicodeHangul().ComposePair(0x1100, 0x0041), Is.EqualTo(-1));
    }

    [Test]
    public void UnicodeNormalization_ExposesHangulAlgorithmWrappers()
    {
        Assert.That(UnicodeNormalization.DecomposeHangulSyllable(0xAC01), Is.EqualTo(new int[] { 0x1100, 0x1161, 0x11A8 }));
        Assert.That(UnicodeNormalization.ComposeHangulPair(0x1100, 0x1161), Is.EqualTo(0xAC00));
        Assert.That(UnicodeNormalization.ComposeHangulPair(0xAC00, 0x11A8), Is.EqualTo(0xAC01));
        Assert.That(UnicodeNormalization.ComposeHangulSyllable(0x1100, 0x1161), Is.EqualTo(0xAC00));
        Assert.That(UnicodeNormalization.ComposeHangulSyllable(0x1100, 0x1161, 0x11A8), Is.EqualTo(0xAC01));
    }
}
