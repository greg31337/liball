using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeDecompositionTests
{
    [Test]
    public void DecomposeCodePointCanonical_ExpandsCanonicalMappingsRecursively()
    {
        Assert.That(UnicodeNormalization.DecomposeCodePointCanonical(0x00C0), Is.EqualTo(new int[] { 0x0041, 0x0300 }));
        Assert.That(UnicodeNormalization.DecomposeCodePointCanonical(0x01DB), Is.EqualTo(new int[] { 0x0055, 0x0308, 0x0300 }));
    }

    [Test]
    public void DecomposeCodePointCanonical_KeepsCompatibilityMappingsUnchanged()
    {
        Assert.That(UnicodeNormalization.DecomposeCodePointCanonical(0x00B2), Is.EqualTo(new int[] { 0x00B2 }));
        Assert.That(UnicodeNormalization.DecomposeCodePointCanonical(0x00BD), Is.EqualTo(new int[] { 0x00BD }));
    }

    [Test]
    public void DecomposeCodePointCompatibility_ExpandsCanonicalAndCompatibilityMappings()
    {
        Assert.That(UnicodeNormalization.DecomposeCodePointCompatibility(0x00C0), Is.EqualTo(new int[] { 0x0041, 0x0300 }));
        Assert.That(UnicodeNormalization.DecomposeCodePointCompatibility(0x00B2), Is.EqualTo(new int[] { 0x0032 }));
        Assert.That(UnicodeNormalization.DecomposeCodePointCompatibility(0x00BD), Is.EqualTo(new int[] { 0x0031, 0x2044, 0x0032 }));
    }

    [Test]
    public void DecomposeCodePoint_UsesHangulAlgorithmForCanonicalAndCompatibilityDecomposition()
    {
        Assert.That(UnicodeNormalization.DecomposeCodePointCanonical(0xAC00), Is.EqualTo(new int[] { 0x1100, 0x1161 }));
        Assert.That(UnicodeNormalization.DecomposeCodePointCanonical(0xAC01), Is.EqualTo(new int[] { 0x1100, 0x1161, 0x11A8 }));
        Assert.That(UnicodeNormalization.DecomposeCodePointCompatibility(0xD7A3), Is.EqualTo(new int[] { 0x1112, 0x1175, 0x11C2 }));
    }

    [Test]
    public void DecomposeCanonical_DecomposesEveryScalarInText()
    {
        Assert.That(UnicodeNormalization.DecomposeCanonical("\u00C0\u01DB\uAC01"), Is.EqualTo("A\u0300U\u0308\u0300\u1100\u1161\u11A8"));
    }

    [Test]
    public void DecomposeCompatibility_DecomposesCompatibilityMappingsInText()
    {
        Assert.That(UnicodeNormalization.DecomposeCompatibility("\u00B2\u00BD"), Is.EqualTo("2" + "\u0031\u2044\u0032"));
    }

    [Test]
    public void Decompose_ReturnsUnmappedScalarsUnchanged()
    {
        Assert.That(UnicodeNormalization.DecomposeCodePointCanonical(0x0041), Is.EqualTo(new int[] { 0x0041 }));
        Assert.That(UnicodeNormalization.DecomposeCanonical("ABC"), Is.EqualTo("ABC"));
    }

    [Test]
    public void Decompose_RejectsNullText()
    {
        Assert.Throws<ArgumentNullException>(() => UnicodeNormalization.DecomposeCanonical(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodeNormalization.DecomposeCompatibility(null!));
    }
}
